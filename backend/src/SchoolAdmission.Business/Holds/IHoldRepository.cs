using SchoolAdmission.Business.Students;

namespace SchoolAdmission.Business.Holds;

public interface IHoldRepository
{
    /// <summary>Marks this student's HELD holds in the class with expires_at in the past as EXPIRED.</summary>
    Task ExpireStaleForStudentAsync(Guid classId, StudentId student, CancellationToken ct);

    /// <summary>True when the student has a live hold in the class: CONFIRMED, or HELD and not yet expired.</summary>
    Task<bool> HasActiveAsync(Guid classId, StudentId student, CancellationToken ct);

    /// <summary>Counts live holds in the class, with the same rule as <see cref="HasActiveAsync"/>.</summary>
    Task<int> CountActiveAsync(Guid classId, CancellationToken ct);

    /// <summary>Inserts a HELD hold that expires after <paramref name="durationSeconds"/>, by database time.</summary>
    Task<HoldRecord> InsertAsync(Guid classId, StudentId student, int durationSeconds, CancellationToken ct);

    /// <summary>
    /// Locks the row of the class this hold belongs to (the same lock a create takes) until the open
    /// transaction ends. False when the hold does not exist. Call it inside an <see cref="IUnitOfWork"/> transaction.
    /// </summary>
    Task<bool> LockClassAsync(Guid holdId, CancellationToken ct);

    /// <summary>Moves the student's unexpired HELD hold to CONFIRMED. False when no row matched.</summary>
    Task<bool> TryConfirmAsync(Guid id, StudentId student, CancellationToken ct);

    /// <summary>Moves the student's unexpired HELD hold to CANCELLED. False when no row matched.</summary>
    Task<bool> TryCancelAsync(Guid id, StudentId student, CancellationToken ct);

    /// <summary>Reads one hold. A HELD hold past its expiry reads as EXPIRED.</summary>
    Task<HoldRecord?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Reads the student's holds, newest first. Same derived status as <see cref="GetAsync"/>.</summary>
    Task<IReadOnlyList<HoldRecord>> ListForStudentAsync(StudentId student, CancellationToken ct);
}
