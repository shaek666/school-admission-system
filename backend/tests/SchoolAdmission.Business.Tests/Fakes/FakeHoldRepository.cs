using SchoolAdmission.Business.Holds;
using SchoolAdmission.Business.Students;

namespace SchoolAdmission.Business.Tests.Fakes;

public sealed class FakeHoldRepository(CallLog log) : IHoldRepository
{
    public bool StudentHasActiveHold { get; set; }

    public int ActiveCount { get; set; }

    public int? InsertedDurationSeconds { get; private set; }

    public Task ExpireStaleForStudentAsync(Guid classId, StudentId student, CancellationToken ct)
    {
        log.Add(CallLog.ExpireStale);
        return Task.CompletedTask;
    }

    public Task<bool> HasActiveAsync(Guid classId, StudentId student, CancellationToken ct)
    {
        log.Add(CallLog.HasActive);
        return Task.FromResult(StudentHasActiveHold);
    }

    public Task<int> CountActiveAsync(Guid classId, CancellationToken ct)
    {
        log.Add(CallLog.CountActive);
        return Task.FromResult(ActiveCount);
    }

    public Task<HoldRecord> InsertAsync(Guid classId, StudentId student, int durationSeconds, CancellationToken ct)
    {
        log.Add(CallLog.Insert);
        InsertedDurationSeconds = durationSeconds;
        var record = new HoldRecord(
            Guid.NewGuid(), classId, "Class 6", student.Value, HoldStatus.Held, DateTime.UtcNow.AddSeconds(durationSeconds));
        return Task.FromResult(record);
    }

    public Task<HoldRecord?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<HoldRecord?>(null);

    public Task<IReadOnlyList<HoldRecord>> ListForStudentAsync(StudentId student, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<HoldRecord>>([]);
}
