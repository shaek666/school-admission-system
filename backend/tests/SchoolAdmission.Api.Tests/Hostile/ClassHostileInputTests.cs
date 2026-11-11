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
