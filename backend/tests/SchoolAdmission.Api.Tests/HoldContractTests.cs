using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

[Collection(PostgresCollection.Name)]
public partial class HoldContractTests(PostgresFixture fixture)
{
    private const string StudentId = "20152691234567890";
    private const int DefaultDurationSeconds = 600;
    private const int ClockToleranceSeconds = 60;

    private static readonly string[] ContractProperties = ["classId", "expiresAt", "holdId", "status", "studentId"];

    [GeneratedRegex(@"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$")]
    private static partial Regex ExpiresAtPattern();

    private static Task<HttpResponseMessage> PostHoldAsync(HttpClient client, string classId, params string[] studentIds)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{classId}/holds");
        foreach (var studentId in studentIds)
        {
            request.Headers.Add("X-Student-Id", studentId);
        }

        return client.SendAsync(request);
    }

    [Fact]
    public async Task Post_hold_returns_201_with_contract_fields()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);

        var response = await PostHoldAsync(client, classId, StudentId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var root = await HttpJson.ReadJsonAsync(response);
        Assert.Equal(ContractProperties, root.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.True(Guid.TryParse(root.GetProperty("holdId").GetString(), out _));
        Assert.Equal(classId, root.GetProperty("classId").GetString());
        Assert.Equal(StudentId, root.GetProperty("studentId").GetString());
        Assert.Equal("HELD", root.GetProperty("status").GetString());

        var expiresAt = root.GetProperty("expiresAt").GetString()!;
        Assert.Matches(ExpiresAtPattern(), expiresAt);
        var parsed = DateTimeOffset.Parse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        var expected = DateTimeOffset.UtcNow.AddSeconds(DefaultDurationSeconds);
        Assert.InRange(Math.Abs((parsed - expected).TotalSeconds), 0, ClockToleranceSeconds);
    }

    [Fact]
    public async Task Post_hold_without_header_is_400_INVALID_STUDENT_ID()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);

        var response = await PostHoldAsync(client, classId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_STUDENT_ID", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Post_hold_without_header_on_unknown_class_is_400_INVALID_STUDENT_ID()
    {
        using var client = fixture.Factory.CreateClient();

        var response = await PostHoldAsync(client, Guid.NewGuid().ToString());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_STUDENT_ID", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Post_hold_with_16_digit_header_is_400_INVALID_STUDENT_ID()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);

        var response = await PostHoldAsync(client, classId, StudentId[..16]);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_STUDENT_ID", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Post_hold_with_repeated_header_is_400_INVALID_STUDENT_ID()
    {
        using var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);

        var response = await PostHoldAsync(client, classId, StudentId, StudentId);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_STUDENT_ID", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Post_hold_on_unknown_class_is_404_CLASS_NOT_FOUND()
    {
        using var client = fixture.Factory.CreateClient();

        var response = await PostHoldAsync(client, Guid.NewGuid().ToString(), StudentId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CLASS_NOT_FOUND", await HttpJson.ErrorCodeAsync(response));
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("..%2F..")]
    [InlineData("00000000000000000000000000000001")]
    public async Task Post_hold_on_malformed_class_id_is_404_CLASS_NOT_FOUND(string classId)
    {
        using var client = fixture.Factory.CreateClient();

        var response = await PostHoldAsync(client, classId, StudentId);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CLASS_NOT_FOUND", await HttpJson.ErrorCodeAsync(response));
    }
}
