using Microsoft.Extensions.Logging;
using SchoolAdmission.Business.Classes;
using SchoolAdmission.Business.Students;

namespace SchoolAdmission.Business.Holds;

public sealed class HoldService(
    IUnitOfWork unitOfWork,
    IClassRepository classes,
    IHoldRepository holds,
    HoldOptions options,
    ILogger<HoldService> logger)
{
    public async Task<HoldRecord> CreateAsync(string classId, StudentId student, CancellationToken ct)
    {
        // One ReadCommitted transaction: lock the class row, expire this student's stale holds,
        // reject an active hold, count live holds, insert, commit. A throw before the commit rolls back.
        // A 23505 on ux_holds_student_class_active also maps to ALREADY_APPLIED as a safety net.
        if (!Ids.TryParse(classId, out var id))
        {
            throw Errors.ClassNotFound();
        }

        await using var tx = await unitOfWork.BeginAsync(ct);
        var totalSeats = await classes.LockAsync(id, ct) ?? throw Errors.ClassNotFound();

        await holds.ExpireStaleForStudentAsync(id, student, ct);
        if (await holds.HasActiveAsync(id, student, ct))
        {
            throw Errors.AlreadyApplied();
        }

        if (await holds.CountActiveAsync(id, ct) >= totalSeats)
        {
            throw Errors.NoSeatsAvailable();
        }

        var hold = await holds.InsertAsync(id, student, options.DurationSeconds, ct);
        await tx.CommitAsync(ct);

        logger.LogInformation("Hold {HoldId} created for student {Student} in class {ClassId}",
            hold.Id, student.Masked, id);
        return hold;
    }

    /// <summary>Stores EXPIRED on stale HELD holds. One UPDATE is atomic, so no transaction. Reads never depend on it.</summary>
    public async Task<int> ExpireStaleAsync(CancellationToken ct)
    {
        var count = await holds.ExpireAllStaleAsync(ct);
        if (count > 0)
        {
            logger.LogInformation("Sweep stored EXPIRED on {Count} stale holds", count);
        }

        return count;
    }

    public async Task<HoldRecord> GetAsync(string holdId, CancellationToken ct)
    {
        if (!Ids.TryParse(holdId, out var id))
        {
            throw Errors.HoldNotFound();
        }

        return await holds.GetAsync(id, ct) ?? throw Errors.HoldNotFound();
    }

    public Task<IReadOnlyList<HoldRecord>> ListMineAsync(StudentId student, CancellationToken ct) =>
        holds.ListForStudentAsync(student, ct);

    public async Task<HoldRecord> ConfirmAsync(string holdId, StudentId student, CancellationToken ct)
    {
        // Under the class lock, the conditional UPDATE is the whole decision when it matches. Either way, read the hold in the
        // same transaction: it is the response body on success, and the input to the state rules on a miss.
        if (!Ids.TryParse(holdId, out var id))
        {
            throw Errors.HoldNotFound();
        }

        // Lock the class row first, as a create does. Otherwise a confirm that commits just after expiry
        // can race a create that already counted this hold as expired, and the class oversells.
        await using var tx = await unitOfWork.BeginAsync(ct);
        if (!await holds.LockClassAsync(id, ct))
        {
            throw Errors.HoldNotFound();
        }

        var matched = await holds.TryConfirmAsync(id, student, ct);
        var hold = await holds.GetAsync(id, ct) ?? throw Errors.HoldNotFound();
        if (matched)
        {
            await tx.CommitAsync(ct);
            logger.LogInformation("Hold {HoldId} confirmed by student {Student}", id, student.Masked);
            return hold;
        }

        EnsureOwner(hold, student);
        return hold.Status switch
        {
            HoldStatus.Confirmed => hold,
            HoldStatus.Cancelled => throw Errors.HoldCancelled(),
            _ => throw Errors.HoldExpired()
        };
    }

    public async Task CancelAsync(string holdId, StudentId student, CancellationToken ct)
    {
        if (!Ids.TryParse(holdId, out var id))
        {
            throw Errors.HoldNotFound();
        }

        await using var tx = await unitOfWork.BeginAsync(ct);
        if (await holds.TryCancelAsync(id, student, ct))
        {
            await tx.CommitAsync(ct);
            logger.LogInformation("Hold {HoldId} cancelled by student {Student}", id, student.Masked);
            return;
        }

        var hold = await holds.GetAsync(id, ct) ?? throw Errors.HoldNotFound();
        EnsureOwner(hold, student);
        if (hold.Status == HoldStatus.Cancelled)
        {
            return;
        }

        throw hold.Status == HoldStatus.Confirmed ? Errors.HoldConfirmed() : Errors.HoldExpired();
    }

    private static void EnsureOwner(HoldRecord hold, StudentId student)
    {
        if (hold.StudentId != student.Value)
        {
            throw Errors.NotHoldOwner();
        }
    }
}
