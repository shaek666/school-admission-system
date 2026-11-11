using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using SchoolAdmission.Business;

namespace SchoolAdmission.DataAccess;

public sealed class UnitOfWork(AdmissionDbContext context, IOptions<DataAccessOptions> options) : IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginAsync(CancellationToken ct)
    {
        // Read Committed on purpose. After the class row lock is granted, the next COUNT takes a fresh
        // snapshot and sees the holds that earlier lock holders committed. Under Repeatable Read, the
        // snapshot would be fixed at the first statement, and a waiting transaction would fail with a
        // serialization error instead.
        var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // set_config(..., true) is SET LOCAL that takes a parameter, so no SQL is built from strings.
            var timeout = options.Value.LockTimeoutSeconds.ToString(CultureInfo.InvariantCulture) + "s";
            await context.Database.ExecuteSqlRawAsync("SELECT set_config('lock_timeout', {0}, true)", [timeout], ct);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }

        return new Transaction(transaction);
    }

    private sealed class Transaction(IDbContextTransaction inner) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken ct) => inner.CommitAsync(ct);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
