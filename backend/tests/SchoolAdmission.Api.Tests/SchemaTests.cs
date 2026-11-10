using Npgsql;
using SchoolAdmission.Api.Tests.Infrastructure;
using SchoolAdmission.DataAccess;

namespace SchoolAdmission.Api.Tests;

[Collection(PostgresCollection.Name)]
public class SchemaTests
{
    [Fact]
    public async Task Partial_unique_index_exists() =>
        Assert.Equal(1L, await Sql.ScalarAsync<long>(
            "select count(*) from pg_indexes where indexname = $1", DbNames.ActiveHoldIndex));

    [Fact]
    public async Task Student_id_check_rejects_16_digits()
    {
        var classId = Guid.NewGuid();
        await Sql.ExecAsync(
            "insert into classes (id, name, total_seats) values ($1, 'Check test', 5)", classId);

        var ex = await Assert.ThrowsAsync<PostgresException>(() => Sql.ExecAsync(
            "insert into holds (id, class_id, student_id, status, expires_at, created_at) " +
            "values ($1, $2, '1234567890123456', 'HELD', now() + interval '1 hour', now())",
            Guid.NewGuid(), classId));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }
}
