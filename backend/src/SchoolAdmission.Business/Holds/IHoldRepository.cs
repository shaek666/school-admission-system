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

    /// <summary>Reads one hold. A HELD hold past its expiry reads as EXPIRED.</summary>
    Task<HoldRecord?> GetAsync(Guid id, CancellationToken ct);

    /// <summary>Reads the student's holds, newest first. Same derived status as <see cref="GetAsync"/>.</summary>
    Task<IReadOnlyList<HoldRecord>> ListForStudentAsync(StudentId student, CancellationToken ct);
}
