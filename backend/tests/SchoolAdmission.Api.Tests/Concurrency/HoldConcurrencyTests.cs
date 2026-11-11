using System.Net;
using Npgsql;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests.Concurrency;

[Collection(ConcurrencyCollection.Name)]
public class HoldConcurrencyTests
{
    private const string HeldCount = "select count(*) from holds where class_id = $1 and status = 'HELD'";
    private static readonly TimeSpan RaceTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan LockTestTimeout = TimeSpan.FromSeconds(30);

    private static async Task<HttpStatusCode[]> RaceAsync(HttpClient client, string classId, IReadOnlyList<string> studentIds,
        Dictionary<string, int>? errorCodes = null)
    {
        var tasks = studentIds.Select(async studentId =>
        {
            using var response = await TestData.SendAsync(client, HttpMethod.Post, $"/classes/{classId}/holds", studentId);
            var code = response.IsSuccessStatusCode ? null : await HttpJson.ErrorCodeAsync(response);
            return (response.StatusCode, code);
        }).ToArray();

        var results = await Task.WhenAll(tasks).WaitAsync(RaceTimeout);
        if (errorCodes is not null)
        {
            foreach (var code in results.Select(r => r.code).OfType<string>())
            {
                errorCodes[code] = errorCodes.GetValueOrDefault(code) + 1;
            }
        }

        return results.Select(r => r.StatusCode).ToArray();
    }

    private static List<string> NewStudents(int count) =>
        Enumerable.Range(0, count).Select(_ => TestData.NewStudentId()).ToList();

    [Fact]
    public async Task Two_hundred_parallel_holds_on_10_seats()
    {
        using var factory = new AppFactory();
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 10);
        var codes = new Dictionary<string, int>();

        var statuses = await RaceAsync(client, classId, NewStudents(200), codes);

        Assert.Equal(10, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(190, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.Equal(190, codes["NO_SEATS_AVAILABLE"]);
        Assert.Single(codes);
        Assert.Equal(10L, await Sql.ScalarAsync<long>(HeldCount, Guid.Parse(classId)));
    }

    [Fact]
    public async Task Same_student_twenty_parallel_holds()
    {
        using var factory = new AppFactory();
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 10);
        var codes = new Dictionary<string, int>();
        var studentId = TestData.NewStudentId();

        var statuses = await RaceAsync(client, classId, Enumerable.Repeat(studentId, 20).ToList(), codes);

        Assert.Equal(1, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(19, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.Equal(19, codes["ALREADY_APPLIED"]);
        Assert.Equal(1L, await Sql.ScalarAsync<long>(HeldCount, Guid.Parse(classId)));
    }

    [Fact]
    public async Task Thousand_parallel_holds_never_503()
    {
        using var factory = new AppFactory();
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 50);

        var statuses = await RaceAsync(client, classId, NewStudents(1000));

        Assert.All(statuses, s => Assert.True(s is HttpStatusCode.Created or HttpStatusCode.Conflict, $"Unexpected status {s}"));
        Assert.Equal(50, statuses.Count(s => s == HttpStatusCode.Created));
        Assert.Equal(950, statuses.Count(s => s == HttpStatusCode.Conflict));
        Assert.Equal(50L, await Sql.ScalarAsync<long>(HeldCount, Guid.Parse(classId)));
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

        Assert.Equal(0L, await Sql.ScalarAsync<long>(HeldCount, Guid.Parse(classId)));
    }
}
