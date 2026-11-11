using Microsoft.Extensions.Logging;
using SchoolAdmission.Business.Classes;
using SchoolAdmission.Business.Students;

namespace SchoolAdmission.Business.Holds;

public sealed class HoldService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClassRepository _classes;
    private readonly IHoldRepository _holds;
    private readonly HoldOptions _options;
    private readonly ILogger<HoldService> _logger;

    public HoldService(
        IUnitOfWork unitOfWork,
        IClassRepository classes,
        IHoldRepository holds,
        HoldOptions options,
        ILogger<HoldService> logger)
    {
        _unitOfWork = unitOfWork;
        _classes = classes;
        _holds = holds;
        _options = options;
        _logger = logger;
    }

    public async Task<HoldRecord> CreateAsync(string classId, StudentId student, CancellationToken ct)
    {
        // One ReadCommitted transaction: lock the class row, expire this student's stale holds,
        // reject an active hold, count live holds, insert, commit. A throw before the commit rolls back.
        // A 23505 on ux_holds_student_class_active also maps to ALREADY_APPLIED as a safety net.
        if (!Ids.TryParse(classId, out var id))
        {
            throw Errors.ClassNotFound();
        }

        await using var tx = await _unitOfWork.BeginAsync(ct);
        var totalSeats = await _classes.LockAsync(id, ct) ?? throw Errors.ClassNotFound();

        await _holds.ExpireStaleForStudentAsync(id, student, ct);
        if (await _holds.HasActiveAsync(id, student, ct))
        {
            throw Errors.AlreadyApplied();
        }

        if (await _holds.CountActiveAsync(id, ct) >= totalSeats)
        {
            throw Errors.NoSeatsAvailable();
        }

        var hold = await _holds.InsertAsync(id, student, _options.DurationSeconds, ct);
        await tx.CommitAsync(ct);

        _logger.LogInformation("Hold {HoldId} created for student {Student} in class {ClassId}",
            hold.Id, student.Masked, id);
        return hold;
    }

    public async Task<HoldRecord> GetAsync(string holdId, CancellationToken ct)
    {
        if (!Ids.TryParse(holdId, out var id))
        {
            throw Errors.HoldNotFound();
        }

        return await _holds.GetAsync(id, ct) ?? throw Errors.HoldNotFound();
    }

    public Task<IReadOnlyList<HoldRecord>> ListMineAsync(StudentId student, CancellationToken ct) =>
        _holds.ListForStudentAsync(student, ct);
}
