namespace SchoolAdmission.Business;

/// <summary>Starts one database transaction. Disposing the transaction without a commit rolls it back.</summary>
public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginAsync(CancellationToken ct);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
