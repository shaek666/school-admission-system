using System.Net;
using System.Text.Json;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

[Collection(PostgresCollection.Name)]
public class ExpiryTests(PostgresFixture fixture)
{
    private const int ClassSeats = 3;

    private static async Task<JsonElement> SeatsAsync(HttpClient client, string classId) =>
        await HttpJson.ReadJsonAsync(await client.GetAsync($"/classes/{classId}"));

    private static async Task<(string ClassId, string Student, string HoldId)> CreateExpiredHoldAsync(HttpClient client)
    {
        var classId = await TestData.CreateClassAsync(client, ClassSeats);
        var student = TestData.NewStudentId();
        var holdId = await TestData.CreateHoldAsync(client, classId, student);
        await Sql.ExpireAsync(Guid.Parse(holdId));
        return (classId, student, holdId);
    }

    [Fact]
    public async Task Expired_hold_reads_EXPIRED_everywhere()
    {
        using var client = fixture.Factory.CreateClient();
        var (_, student, holdId) = await CreateExpiredHoldAsync(client);

        var single = await HttpJson.ReadJsonAsync(await TestData.SendAsync(client, HttpMethod.Get, $"/holds/{holdId}"));
        var mine = await HttpJson.ReadJsonAsync(await TestData.SendAsync(client, HttpMethod.Get, "/me/holds", student));

        Assert.Equal("EXPIRED", single.GetProperty("status").GetString());
        var listed = mine.EnumerateArray().Single(h => h.GetProperty("holdId").GetString() == holdId);
        Assert.Equal("EXPIRED", listed.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Expired_hold_lowers_heldSeats_and_raises_availableSeats()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, ClassSeats);
        var holdId = await TestData.CreateHoldAsync(client, classId, TestData.NewStudentId());
        var before = await SeatsAsync(client, classId);
        await Sql.ExpireAsync(Guid.Parse(holdId));

        var after = await SeatsAsync(client, classId);

        Assert.Equal(1, before.GetProperty("heldSeats").GetInt32());
        Assert.Equal(ClassSeats - 1, before.GetProperty("availableSeats").GetInt32());
        Assert.Equal(0, after.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, after.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(ClassSeats, after.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task Expired_student_can_apply_again()
    {
        using var client = fixture.Factory.CreateClient();
        var (classId, student, first) = await CreateExpiredHoldAsync(client);

        var second = await TestData.CreateHoldAsync(client, classId, student);

        Assert.NotEqual(first, second);
        var seats = await SeatsAsync(client, classId);
        Assert.Equal(1, seats.GetProperty("heldSeats").GetInt32());
    }

    [Theory]
    [InlineData("POST", "/confirm")]
    [InlineData("DELETE", "")]
    public async Task Confirm_or_delete_expired_is_409_HOLD_EXPIRED(string method, string suffix)
    {
        using var client = fixture.Factory.CreateClient();
        var (_, student, holdId) = await CreateExpiredHoldAsync(client);

        var response = await TestData.SendAsync(client, new HttpMethod(method), $"/holds/{holdId}{suffix}", student);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("HOLD_EXPIRED", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Hold_duration_env_is_used()
    {
        using var factory = new AppFactory().WithSetting("HOLD_DURATION_SECONDS", "1");
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, ClassSeats);
        var holdId = await TestData.CreateHoldAsync(client, classId, TestData.NewStudentId());

        var seconds = await Sql.ScalarAsync<int>(
            "select extract(epoch from expires_at - date_trunc('second', created_at))::int from holds where id = $1",
            Guid.Parse(holdId));
        Assert.Equal(1, seconds);
    }
}
