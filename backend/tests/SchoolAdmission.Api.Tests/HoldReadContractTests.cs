using System.Net;
using System.Text.Json;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

[Collection(PostgresCollection.Name)]
public class HoldReadContractTests(PostgresFixture fixture)
{
    private static readonly string[] HoldContractProperties = ["classId", "expiresAt", "holdId", "status"];
    private static readonly string[] MyHoldContractProperties = ["classId", "className", "expiresAt", "holdId", "status"];

    private static async Task<JsonElement> PostHoldAsync(HttpClient client, string classId, string studentId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        request.Headers.Add("X-Student-Id", studentId);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await HttpJson.ReadJsonAsync(response);
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string? studentId = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (studentId is not null)
        {
            request.Headers.Add("X-Student-Id", studentId);
        }

        return await client.SendAsync(request);
    }

    [Fact]
    public async Task Get_hold_returns_contract_fields_without_studentId()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);
        var created = await PostHoldAsync(client, classId, TestData.NewStudentId());

        var response = await GetAsync(client, $"/holds/{created.GetProperty("holdId").GetString()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await HttpJson.ReadJsonAsync(response);
        Assert.Equal(HoldContractProperties, root.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(created.GetProperty("holdId").GetString(), root.GetProperty("holdId").GetString());
        Assert.Equal(classId, root.GetProperty("classId").GetString());
        Assert.Equal("HELD", root.GetProperty("status").GetString());
        Assert.Equal(created.GetProperty("expiresAt").GetString(), root.GetProperty("expiresAt").GetString());
    }

    [Fact]
    public async Task Get_hold_needs_no_header_and_ignores_other_student()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);
        var created = await PostHoldAsync(client, classId, TestData.NewStudentId());
        var url = $"/holds/{created.GetProperty("holdId").GetString()}";

        var withoutHeader = await GetAsync(client, url);
        var otherStudent = await GetAsync(client, url, TestData.NewStudentId());
        var badHeader = await GetAsync(client, url, "123");

        Assert.Equal(HttpStatusCode.OK, withoutHeader.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherStudent.StatusCode);
        Assert.Equal(HttpStatusCode.OK, badHeader.StatusCode);
    }

    [Fact]
    public async Task Get_hold_past_expiry_reads_as_EXPIRED()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);
        var created = await PostHoldAsync(client, classId, TestData.NewStudentId());
        var holdId = created.GetProperty("holdId").GetString()!;
        await Sql.ExecAsync("update holds set expires_at = now() - interval '1 minute' where id = $1", Guid.Parse(holdId));

        var root = await HttpJson.ReadJsonAsync(await GetAsync(client, $"/holds/{holdId}"));

        Assert.Equal("EXPIRED", root.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-guid")]
    [InlineData("..%2F..")]
    [InlineData("00000000000000000000000000000001")]
    [InlineData("%20")]
    public async Task Get_unknown_or_malformed_hold_is_404_HOLD_NOT_FOUND(string holdId)
    {
        using var client = fixture.Factory.CreateClient();

        var response = await GetAsync(client, $"/holds/{holdId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("HOLD_NOT_FOUND", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Me_holds_returns_array_newest_first_with_className()
    {
        using var client = fixture.Factory.CreateClient();
        var student = TestData.NewStudentId();
        var first = await TestData.CreateClassAsync(client, 5, "First class " + Guid.NewGuid());
        var second = await TestData.CreateClassAsync(client, 5, "Second class " + Guid.NewGuid());
        var oldest = await PostHoldAsync(client, first, student);
        var newest = await PostHoldAsync(client, second, student);

        var response = await GetAsync(client, "/me/holds", student);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = (await HttpJson.ReadJsonAsync(response)).EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(newest.GetProperty("holdId").GetString(), items[0].GetProperty("holdId").GetString());
        Assert.Equal(oldest.GetProperty("holdId").GetString(), items[1].GetProperty("holdId").GetString());
        Assert.Equal(MyHoldContractProperties, items[0].EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(second, items[0].GetProperty("classId").GetString());
        Assert.StartsWith("Second class", items[0].GetProperty("className").GetString());
        Assert.StartsWith("First class", items[1].GetProperty("className").GetString());
        Assert.Equal("HELD", items[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Me_holds_lists_only_the_callers_holds()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);
        var mine = TestData.NewStudentId();
        await PostHoldAsync(client, classId, mine);
        await PostHoldAsync(client, classId, TestData.NewStudentId());

        var items = (await HttpJson.ReadJsonAsync(await GetAsync(client, "/me/holds", mine))).EnumerateArray().ToList();

        Assert.Single(items);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1234567890123456")]
    public async Task Me_holds_without_valid_header_is_400(string? header)
    {
        using var client = fixture.Factory.CreateClient();

        var response = await GetAsync(client, "/me/holds", header);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_STUDENT_ID", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Me_holds_for_new_student_is_empty_array()
    {
        using var client = fixture.Factory.CreateClient();

        var response = await GetAsync(client, "/me/holds", TestData.NewStudentId());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await HttpJson.ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(0, root.GetArrayLength());
    }

    [Fact]
    public async Task Data_survives_a_new_factory()
    {
        string holdId;
        string classId;
        using (var firstFactory = new AppFactory())
        {
            using var client = firstFactory.CreateClient();
            classId = await TestData.CreateClassAsync(client, 5);
            holdId = (await PostHoldAsync(client, classId, TestData.NewStudentId())).GetProperty("holdId").GetString()!;
        }

        using var secondFactory = new AppFactory();
        using var secondClient = secondFactory.CreateClient();
        var response = await GetAsync(secondClient, $"/holds/{holdId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var root = await HttpJson.ReadJsonAsync(response);
        Assert.Equal(classId, root.GetProperty("classId").GetString());
        Assert.Equal("HELD", root.GetProperty("status").GetString());
    }
}
