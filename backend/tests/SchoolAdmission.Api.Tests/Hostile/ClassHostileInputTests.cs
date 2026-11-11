using System.Net;
using System.Text;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests.Hostile;

[Collection(PostgresCollection.Name)]
public class ClassHostileInputTests(PostgresFixture fixture)
{
    private static readonly string LongName = new('x', 5000);

    public static TheoryData<string, string> BadBodies => new()
    {
        { "{", "INVALID_JSON" },
        { "", "INVALID_JSON" },
        { "[]", "INVALID_JSON" },
        { "null", "INVALID_JSON" },
        { """{"name":"A","totalSeats":"7"}""", "INVALID_JSON" },
        { """{"name":"A\u0000B","totalSeats":5}""", "VALIDATION_ERROR" },
        { "{}", "VALIDATION_ERROR" },
        { """{"name":"A","totalSeats":1.5}""", "INVALID_JSON" },
        { """{"name":"A","totalSeats":"abc"}""", "INVALID_JSON" },
        { """{"name":"A","totalSeats":2147483648}""", "INVALID_JSON" },
        { """{"name":"A","totalSeats":0}""", "VALIDATION_ERROR" },
        { """{"name":"   ","totalSeats":5}""", "VALIDATION_ERROR" },
        { $$"""{"name":"{{LongName}}","totalSeats":5}""", "VALIDATION_ERROR" }
    };

    [Theory]
    [MemberData(nameof(BadBodies))]
    public async Task Bad_post_body_returns_400_with_error_code(string body, string code)
    {
        using var client = fixture.Factory.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/classes", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(code, await HttpJson.ErrorCodeAsync(response));
        Assert.DoesNotContain(LongName, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_charset_returns_400_invalid_json()
    {
        using var client = fixture.Factory.CreateClient();
        using var content = new StringContent("""{"name":"A","totalSeats":5}""", Encoding.UTF8);
        content.Headers.Remove("Content-Type");
        content.Headers.TryAddWithoutValidation("Content-Type", "application/json; charset=bogus");

        var response = await client.PostAsync("/classes", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_JSON", await HttpJson.ErrorCodeAsync(response));
    }

    // A %00 path never reaches the app through TestServer (its client throws first), so the business tests cover NUL ids.
    public static TheoryData<string> BadIds => new()
    {
        "{N}",
        "%7B{D}%7D",
        new string('9', 5000),
        "..%2F..",
    };

    [Theory]
    [MemberData(nameof(BadIds))]
    public async Task Bad_class_id_returns_404_class_not_found(string template)
    {
        using var client = fixture.Factory.CreateClient();
        using var content = new StringContent("""{"name":"Ids","totalSeats":5}""", Encoding.UTF8, "application/json");
        var created = await HttpJson.ReadJsonAsync(await client.PostAsync("/classes", content));
        var id = Guid.Parse(created.GetProperty("id").GetString()!);
        var path = template.Replace("{N}", id.ToString("N")).Replace("{D}", id.ToString("D"));

        var response = await client.GetAsync($"/classes/{path}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("CLASS_NOT_FOUND", await HttpJson.ErrorCodeAsync(response));
    }

    [Theory]
    [InlineData("POST", "text/plain", HttpStatusCode.UnsupportedMediaType)]
    [InlineData("PUT", "application/json", HttpStatusCode.MethodNotAllowed)]
    public async Task Wrong_content_type_or_method_returns_error_body(
        string method, string contentType, HttpStatusCode expected)
    {
        using var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/classes")
        {
            Content = new StringContent("""{"name":"A","totalSeats":5}""", Encoding.UTF8, contentType),
        };

        var response = await client.SendAsync(request);

        Assert.Equal(expected, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(await HttpJson.ErrorCodeAsync(response)));
    }
}
