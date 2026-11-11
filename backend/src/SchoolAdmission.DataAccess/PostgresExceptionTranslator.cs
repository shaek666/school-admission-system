using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Business;

namespace SchoolAdmission.DataAccess;

public sealed class PostgresExceptionTranslator : IExceptionTranslator
{
    private const string PoolExhaustedText = "The connection pool has been exhausted";

    public AppException? Translate(Exception exception)
    {
        var current = Unwrap(exception);

        return current switch
        {
            PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: DbNames.ActiveHoldIndex }
                => Errors.AlreadyApplied(),
            PostgresException { SqlState: PostgresErrorCodes.LockNotAvailable } => Errors.ServiceBusy(),
            NpgsqlException npgsql when IsPoolTimeout(npgsql) => Errors.ServiceBusy(),
            _ => null
        };
    }

    // EF wraps the database error in DbUpdateException, or in InvalidOperationException when the
    // provider marks it transient (a lock timeout, for example). The real error is the inner one.
    private static Exception Unwrap(Exception exception) =>
        exception is DbUpdateException or InvalidOperationException { InnerException: NpgsqlException or PostgresException }
            && exception.InnerException is { } inner
            ? inner
            : exception;

    private static bool IsPoolTimeout(NpgsqlException exception) =>
        exception.InnerException is TimeoutException
        || exception.Message.Contains(PoolExhaustedText, StringComparison.OrdinalIgnoreCase);
}
