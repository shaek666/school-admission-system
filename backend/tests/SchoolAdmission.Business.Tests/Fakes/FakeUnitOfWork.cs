namespace SchoolAdmission.Business.Tests.Fakes;

public sealed class FakeUnitOfWork(CallLog log) : IUnitOfWork
{
    public Task<IUnitOfWorkTransaction> BeginAsync(CancellationToken ct)
    {
        log.Add(CallLog.Begin);
        return Task.FromResult<IUnitOfWorkTransaction>(new FakeTransaction(log));
    }

    private sealed class FakeTransaction(CallLog log) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken ct)
        {
            log.Add(CallLog.Commit);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            log.Add(CallLog.Dispose);
            return ValueTask.CompletedTask;
        }
    }
}
