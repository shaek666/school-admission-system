using System.Net;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests.Concurrency;

[Collection(ConcurrencyCollection.Name)]
public class ConfirmExpiryConcurrencyTests
{
    private const int ParallelConfirms = 20;
    private static readonly TimeSpan ExpiryWait = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);

    private static async Task WaitUntilExpiredAsync(HttpClient client, string holdId)
    {
        using var cts = new CancellationTokenSource(ExpiryWait);
        while (true)
        {
            var root = await HttpJson.ReadJsonAsync(await client.GetAsync($"/holds/{holdId}", cts.Token));
            if (root.GetProperty("status").GetString() == "EXPIRED")
            {
                return;
            }

            await Task.Delay(PollInterval, cts.Token);
        }
    }

    private static async Task<(string HoldId, string StudentId)> ExpiredHoldAsync(HttpClient client)
    {
        var classId = await TestData.CreateClassAsync(client, 2);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);
        await WaitUntilExpiredAsync(client, holdId);
        return (holdId, student);
    }

    [Fact]
    public async Task Twenty_parallel_confirms_all_200_and_confirmedSeats_is_1()
    {
        using var factory = new AppFactory();
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 3);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);

        var statuses = await Task.WhenAll(Enumerable.Range(0, ParallelConfirms).Select(async _ =>
        {
            using var response = await TestData.SendAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", student);
            return response.StatusCode;
        }));

        Assert.All(statuses, s => Assert.Equal(HttpStatusCode.OK, s));
        var seats = await HttpJson.ReadJsonAsync(await client.GetAsync($"/classes/{classId}"));
        Assert.Equal(1, seats.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, seats.GetProperty("heldSeats").GetInt32());
    }

    [Fact]
    public async Task Confirm_expired_hold_is_409_HOLD_EXPIRED()
    {
        using var factory = new AppFactory().WithSetting("HOLD_DURATION_SECONDS", "1");
        using var client = factory.CreateClient();
        var (holdId, student) = await ExpiredHoldAsync(client);

        var response = await TestData.SendAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", student);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("HOLD_EXPIRED", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Delete_expired_hold_is_409_HOLD_EXPIRED()
    {
        using var factory = new AppFactory().WithSetting("HOLD_DURATION_SECONDS", "1");
        using var client = factory.CreateClient();
        var (holdId, student) = await ExpiredHoldAsync(client);

        var response = await TestData.SendAsync(client, HttpMethod.Delete, $"/holds/{holdId}", student);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("HOLD_EXPIRED", await HttpJson.ErrorCodeAsync(response));
    }
}
