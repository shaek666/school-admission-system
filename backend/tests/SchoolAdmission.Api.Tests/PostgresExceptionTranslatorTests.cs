using Microsoft.EntityFrameworkCore;
using Npgsql;
using SchoolAdmission.DataAccess;

namespace SchoolAdmission.Api.Tests;

public class PostgresExceptionTranslatorTests
{
    private readonly PostgresExceptionTranslator _translator = new();

    private static PostgresException Pg(string sqlState, string? constraint = null) =>
        new("failed", "ERROR", "ERROR", sqlState, constraintName: constraint);

    [Fact]
    public void Active_hold_unique_violation_gives_already_applied()
    {
        var result = _translator.Translate(Pg(PostgresErrorCodes.UniqueViolation, DbNames.ActiveHoldIndex));

        Assert.Equal("ALREADY_APPLIED", result?.Code);
    }

    [Fact]
    public void Other_constraint_gives_null()
    {
        Assert.Null(_translator.Translate(Pg(PostgresErrorCodes.UniqueViolation, "some_other_index")));
    }

    [Fact]
    public void Lock_timeout_gives_service_busy()
    {
        Assert.Equal("SERVICE_BUSY", _translator.Translate(Pg(PostgresErrorCodes.LockNotAvailable))?.Code);
    }

    [Fact]
    public void Transient_wrapper_around_lock_timeout_gives_service_busy()
    {
        var wrapped = new InvalidOperationException("transient failure", Pg(PostgresErrorCodes.LockNotAvailable));

        Assert.Equal("SERVICE_BUSY", _translator.Translate(wrapped)?.Code);
    }

    public static TheoryData<NpgsqlException> PoolTimeouts => new()
    {
        new NpgsqlException("timeout", new TimeoutException()),
        new NpgsqlException("The connection pool has been exhausted. Set Max Pool Size higher."),
    };

    [Theory]
    [MemberData(nameof(PoolTimeouts))]
    public void Pool_timeout_gives_service_busy(NpgsqlException exception)
    {
        Assert.Equal("SERVICE_BUSY", _translator.Translate(exception)?.Code);
    }

    [Fact]
    public void Db_update_exception_is_unwrapped()
    {
        var ex = new DbUpdateException("save", Pg(PostgresErrorCodes.UniqueViolation, DbNames.ActiveHoldIndex));

        Assert.Equal("ALREADY_APPLIED", _translator.Translate(ex)?.Code);
    }

    [Fact]
    public void Unknown_exception_gives_null()
    {
        Assert.Null(_translator.Translate(new InvalidOperationException("x")));
        Assert.Null(_translator.Translate(new NpgsqlException("other")));
    }
}
