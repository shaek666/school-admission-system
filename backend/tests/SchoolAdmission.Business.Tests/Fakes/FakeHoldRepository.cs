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

    /// <summary>What ExpireAllStaleAsync reports.</summary>
    public int ExpireAllCount { get; set; }

    public Task<int> ExpireAllStaleAsync(CancellationToken ct)
    {
        log.Add(CallLog.ExpireAll);
        return Task.FromResult(ExpireAllCount);
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

    /// <summary>The hold GetAsync returns. Null means not found.</summary>
    public HoldRecord? Stored { get; set; }

    /// <summary>What the conditional UPDATEs report. Tests set it to true for the 1 row path.</summary>
    public bool UpdateMatches { get; set; }

    public Task<bool> LockClassAsync(Guid holdId, CancellationToken ct)
    {
        log.Add(CallLog.Lock);
        return Task.FromResult(Stored is not null);
    }

    public Task<bool> TryConfirmAsync(Guid id, StudentId student, CancellationToken ct)
    {
        log.Add(CallLog.TryConfirm);
        return Task.FromResult(UpdateMatches);
    }

    public Task<bool> TryCancelAsync(Guid id, StudentId student, CancellationToken ct)
    {
        log.Add(CallLog.TryCancel);
        return Task.FromResult(UpdateMatches);
    }

    public Task<HoldRecord?> GetAsync(Guid id, CancellationToken ct)
    {
        log.Add(CallLog.Get);
        return Task.FromResult(Stored);
    }

    public Task<IReadOnlyList<HoldRecord>> ListForStudentAsync(StudentId student, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<HoldRecord>>([]);
}
