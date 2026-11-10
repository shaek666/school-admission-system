using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.Business;

namespace SchoolAdmission.DataAccess;

public sealed class PostgresExceptionTranslator : IExceptionTranslator
{
    private const string PoolExhaustedText = "The connection pool has been exhausted";

    public AppException? Translate(Exception exception)
    {
        var current = exception is DbUpdateException { InnerException: { } inner } ? inner : exception;

        return current switch
        {
            PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: DbNames.ActiveHoldIndex }
                => Errors.AlreadyApplied(),
            PostgresException { SqlState: PostgresErrorCodes.LockNotAvailable } => Errors.ServiceBusy(),
            NpgsqlException npgsql when IsPoolTimeout(npgsql) => Errors.ServiceBusy(),
            _ => null
        };
    }

    private static bool IsPoolTimeout(NpgsqlException exception) =>
        exception.InnerException is TimeoutException
        || exception.Message.Contains(PoolExhaustedText, StringComparison.OrdinalIgnoreCase);
}
