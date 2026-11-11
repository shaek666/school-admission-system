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

    [Theory]
    [InlineData(DbNames.StudentCreatedIndex)]
    [InlineData(DbNames.ClassStatusIndex)]
    public async Task Hold_index_exists(string name) =>
        Assert.Equal(1L, await Sql.ScalarAsync<long>("select count(*) from pg_indexes where indexname = $1", name));

    [Theory]
    [InlineData(DbNames.TotalSeatsCheck)]
    [InlineData(DbNames.ClassNameCheck)]
    public async Task Class_check_constraint_exists(string name) =>
        Assert.Equal(1L, await Sql.ScalarAsync<long>(
            "select count(*) from pg_constraint where conname = $1 and contype = 'c'", name));

    [Fact]
    public async Task Student_created_index_orders_created_at_descending() =>
        Assert.Contains("created_at DESC", await Sql.ScalarAsync<string>(
            "select indexdef from pg_indexes where indexname = $1", DbNames.StudentCreatedIndex));

    [Theory]
    [InlineData(0, "Valid name")]
    [InlineData(100001, "Valid name")]
    [InlineData(5, "")]
    public async Task Class_checks_reject_out_of_range_rows(int seats, string name)
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(() => Sql.ExecAsync(
            "insert into classes (id, name, total_seats) values ($1, $2, $3)", Guid.NewGuid(), name, seats));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }

    [Fact]
    public async Task Class_name_check_rejects_201_characters()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(() => Sql.ExecAsync(
            "insert into classes (id, name, total_seats) values ($1, $2, 5)", Guid.NewGuid(), new string('a', 201)));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }

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
