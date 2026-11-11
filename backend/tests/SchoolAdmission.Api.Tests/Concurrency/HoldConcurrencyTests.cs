using System.Globalization;
using System.Net;
using Npgsql;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests.Concurrency;

[Collection(ConcurrencyCollection.Name)]
public class HoldConcurrencyTests
{
    private static readonly TimeSpan LockTestTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Two_hundred_parallel_holds_on_10_seats()
    {
        using var factory = new AppFactory();
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 10);

        var results = await HoldRace.RunAsync([client], classId, HoldRace.NewStudents(200));

        Assert.Equal(10, results.Count(r => r.Status == HttpStatusCode.Created));
        Assert.Equal(190, results.Count(r => r.Status == HttpStatusCode.Conflict));
        Assert.Equal(190, results.Count(r => r.Code == "NO_SEATS_AVAILABLE"));
        Assert.Equal(10L, await Sql.ScalarAsync<long>(HoldRace.HeldCountSql, Guid.Parse(classId)));
    }

    [Fact]
    public async Task Same_student_twenty_parallel_holds()
    {
        using var factory = new AppFactory();
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 10);
        var studentId = TestData.NewStudentId();

        var results = await HoldRace.RunAsync([client], classId, Enumerable.Repeat(studentId, 20).ToList());

        Assert.Equal(1, results.Count(r => r.Status == HttpStatusCode.Created));
        Assert.Equal(19, results.Count(r => r.Status == HttpStatusCode.Conflict));
        Assert.Equal(19, results.Count(r => r.Code == "ALREADY_APPLIED"));
        Assert.Equal(1L, await Sql.ScalarAsync<long>(HoldRace.HeldCountSql, Guid.Parse(classId)));
    }

    [Fact]
    public async Task Thousand_parallel_holds_never_503()
    {
        using var factory = new AppFactory();
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 50);

        var results = await HoldRace.RunAsync([client], classId, HoldRace.NewStudents(1000));

        Assert.All(results, r => Assert.True(r.Status is HttpStatusCode.Created or HttpStatusCode.Conflict, $"Unexpected status {r.Status}"));
        Assert.Equal(50, results.Count(r => r.Status == HttpStatusCode.Created));
        Assert.Equal(950, results.Count(r => r.Status == HttpStatusCode.Conflict));
        Assert.Equal(950, results.Count(r => r.Code == "NO_SEATS_AVAILABLE"));
        Assert.Equal(50L, await Sql.ScalarAsync<long>(HoldRace.HeldCountSql, Guid.Parse(classId)));
    }

    [Fact]
    public async Task Hold_created_after_a_lock_wait_expires_after_the_wait()
    {
        // The hold time starts when the insert runs, not when the transaction began. With now(), a request
        // that waited longer than HOLD_DURATION_SECONDS for the class lock got a hold that was already dead.
        using var factory = new AppFactory().WithSetting("HOLD_DURATION_SECONDS", "1");
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);

        await using var blocker = new NpgsqlConnection(AppFactory.ConnectionString);
        await blocker.OpenAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        await using (var lockCommand = new NpgsqlCommand("select id from classes where id = @id for update", blocker, transaction))
        {
            lockCommand.Parameters.AddWithValue("id", Guid.Parse(classId));
            await lockCommand.ExecuteScalarAsync();
        }

        var pending = TestData.SendAsync(client, HttpMethod.Post, $"/classes/{classId}/holds", TestData.NewStudentId());
        await Task.Delay(TimeSpan.FromSeconds(2.5));
        DateTime releasedAt;
        await using (var clock = new NpgsqlCommand("select clock_timestamp()", blocker, transaction))
        {
            releasedAt = ((DateTime)(await clock.ExecuteScalarAsync())!).ToUniversalTime();
        }

        await transaction.RollbackAsync();
        using var response = await pending.WaitAsync(LockTestTimeout);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await HttpJson.ReadJsonAsync(response);
        Assert.Equal("HELD", body.GetProperty("status").GetString());
        var expiresAt = DateTimeOffset.Parse(body.GetProperty("expiresAt").GetString()!, CultureInfo.InvariantCulture);
        Assert.True(expiresAt.UtcDateTime > releasedAt, $"expiresAt {expiresAt:O} is not after the lock release {releasedAt:O}");
    }

    [Fact]
    public async Task Lock_timeout_returns_503_SERVICE_BUSY()
    {
        using var factory = new AppFactory().WithSetting("DataAccess:LockTimeoutSeconds", "1");
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);

        await using var blocker = new NpgsqlConnection(AppFactory.ConnectionString);
        await blocker.OpenAsync();
        await using var transaction = await blocker.BeginTransactionAsync();
        await using (var lockCommand = new NpgsqlCommand("select id from classes where id = @id for update", blocker, transaction))
        {
            lockCommand.Parameters.AddWithValue("id", Guid.Parse(classId));
            await lockCommand.ExecuteScalarAsync();
        }

        try
        {
            using var response = await TestData
                .SendAsync(client, HttpMethod.Post, $"/classes/{classId}/holds", TestData.NewStudentId())
                .WaitAsync(LockTestTimeout);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("SERVICE_BUSY", await HttpJson.ErrorCodeAsync(response));
            Assert.True(response.Headers.Contains("Retry-After"));
        }
        finally
        {
            await transaction.RollbackAsync();
        }

        Assert.Equal(0L, await Sql.ScalarAsync<long>(HoldRace.HeldCountSql, Guid.Parse(classId)));
    }
}
