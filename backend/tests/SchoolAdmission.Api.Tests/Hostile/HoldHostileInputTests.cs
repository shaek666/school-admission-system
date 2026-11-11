using System.Net;
using System.Text;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests.Hostile;

[Collection(PostgresCollection.Name)]
public class HoldHostileInputTests(PostgresFixture fixture)
{
    private const string HeaderName = "X-Student-Id";

    // A null entry means no header. Two entries mean two header values.
    public static TheoryData<string, string[]?> BadHeaders => new()
    {
        { "none", null },
        { "16 digits", ["2015269123456789"] },
        { "18 digits", ["201526912345678901"] },
        { "letters", ["2015269123456789a"] },
        { "bengali digits", ["২০১৫২৬৯১২৩৪৫৬৭৮৯০"] },
        { "leading space", [" 20152691234567890"] },
        { "two values", ["20152691234567890", "20152691234567891"] },
    };

    public static TheoryData<string> BadIds => new()
    {
        "not-a-guid",
        "{N}",
        "%7B{D}%7D",
        new string('9', 500),
    };

    private static HttpRequestMessage Build(HttpMethod method, string path, string[]? header)
    {
        var request = new HttpRequestMessage(method, path);
        if (header is not null)
        {
            Assert.True(request.Headers.TryAddWithoutValidation(HeaderName, header));
        }

        return request;
    }

    private static string Resolve(string template, string id)
    {
        var guid = Guid.Parse(id);
        return template.Replace("{N}", guid.ToString("N")).Replace("{D}", guid.ToString("D"));
    }

    private async Task<(HttpClient Client, string ClassId, string HoldId, string StudentId)> NewHoldAsync()
    {
        var client = fixture.Factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, 5);
        var studentId = TestData.NewStudentId();
        using var request = Build(HttpMethod.Post, $"/classes/{classId}/holds", [studentId]);
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var holdId = (await HttpJson.ReadJsonAsync(response)).GetProperty("holdId").GetString()!;
        return (client, classId, holdId, studentId);
    }

    private static async Task AssertBadHeaderAsync(HttpClient client, HttpMethod method, string path, string[]? header)
    {
        using var request = Build(method, path, header);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_STUDENT_ID", await HttpJson.ErrorCodeAsync(response));
    }

    [Theory]
    [MemberData(nameof(BadHeaders))]
    public async Task Post_hold_with_bad_header_returns_400(string label, string[]? header)
    {
        var (client, classId, _, _) = await NewHoldAsync();
        using var _c = client;

        await AssertBadHeaderAsync(client, HttpMethod.Post, $"/classes/{classId}/holds", header);
        Assert.NotEmpty(label);
    }

    [Theory]
    [MemberData(nameof(BadHeaders))]
    public async Task Confirm_with_bad_header_returns_400(string label, string[]? header)
    {
        var (client, _, holdId, _) = await NewHoldAsync();
        using var _c = client;

        await AssertBadHeaderAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", header);
        Assert.NotEmpty(label);
    }

    [Theory]
    [MemberData(nameof(BadHeaders))]
    public async Task Delete_with_bad_header_returns_400(string label, string[]? header)
    {
        var (client, _, holdId, _) = await NewHoldAsync();
        using var _c = client;

        await AssertBadHeaderAsync(client, HttpMethod.Delete, $"/holds/{holdId}", header);
        Assert.NotEmpty(label);
    }

    [Theory]
    [MemberData(nameof(BadHeaders))]
    public async Task My_holds_with_bad_header_returns_400(string label, string[]? header)
    {
        using var client = fixture.Factory.CreateClient();

        await AssertBadHeaderAsync(client, HttpMethod.Get, "/me/holds", header);
        Assert.NotEmpty(label);
    }

    [Theory]
    [MemberData(nameof(BadIds))]
    public async Task Bad_hold_id_returns_404_hold_not_found(string template)
    {
        var (client, _, holdId, _) = await NewHoldAsync();
        using var _c = client;
        var path = Resolve(template, holdId);
        string[] header = [TestData.NewStudentId()];

        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Delete })
        {
            var suffix = method == HttpMethod.Post ? "/confirm" : "";
            using var request = Build(method, $"/holds/{path}{suffix}", header);

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("HOLD_NOT_FOUND", await HttpJson.ErrorCodeAsync(response));
        }
    }

    [Fact]
    public async Task Unknown_hold_id_returns_404_hold_not_found()
    {
        using var client = fixture.Factory.CreateClient();
        var path = Guid.NewGuid();
        string[] header = [TestData.NewStudentId()];

        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Delete })
        {
            var suffix = method == HttpMethod.Post ? "/confirm" : "";
            using var request = Build(method, $"/holds/{path}{suffix}", header);

            var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("HOLD_NOT_FOUND", await HttpJson.ErrorCodeAsync(response));
        }
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("{N}")]
    [InlineData("%7B{D}%7D")]
    [InlineData("500")]
    public async Task Bad_class_id_on_post_hold_returns_404_class_not_found(string template)
    {
        var (client, classId, _, _) = await NewHoldAsync();
        using var _c = client;
        var path = template == "500" ? new string('9', 500) : Resolve(template, classId);
        using var request = Build(HttpMethod.Post, $"/classes/{path}/holds", [TestData.NewStudentId()]);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CLASS_NOT_FOUND", await HttpJson.ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Confirm_with_broken_json_body_is_not_500()
    {
        var (client, _, holdId, studentId) = await NewHoldAsync();
        using var _c = client;
        using var request = Build(HttpMethod.Post, $"/holds/{holdId}/confirm", [studentId]);
        request.Content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request);

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest });
    }

    [Fact]
    public async Task Confirm_with_text_plain_body_is_not_500()
    {
        var (client, _, holdId, studentId) = await NewHoldAsync();
        using var _c = client;
        using var request = Build(HttpMethod.Post, $"/holds/{holdId}/confirm", [studentId]);
        request.Content = new StringContent("hello", Encoding.UTF8, "text/plain");

        var response = await client.SendAsync(request);

        Assert.True((int)response.StatusCode < 500, $"Got {(int)response.StatusCode}");
    }
}
