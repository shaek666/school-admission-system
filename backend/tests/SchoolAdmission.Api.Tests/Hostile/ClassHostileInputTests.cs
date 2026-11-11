using System.Net;
using System.Text;
using System.Text.Json;
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
        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        Assert.Equal(code, doc.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.DoesNotContain(LongName, text);
    }

    [Fact]
    public async Task Text_plain_content_type_returns_415_with_error_body()
    {
        using var client = fixture.Factory.CreateClient();
        using var content = new StringContent("""{"name":"A","totalSeats":5}""", Encoding.UTF8, "text/plain");

        var response = await client.PostAsync("/classes", content);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        await AssertErrorBodyAsync(response);
    }

    [Fact]
    public async Task Put_classes_returns_405_with_error_body()
    {
        using var client = fixture.Factory.CreateClient();
        using var content = new StringContent("{}", Encoding.UTF8, "application/json");

        var response = await client.PutAsync("/classes", content);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        await AssertErrorBodyAsync(response);
    }

    [Fact]
    public async Task Post_201_body_has_exactly_the_contract_properties()
    {
        using var client = fixture.Factory.CreateClient();
        using var content = new StringContent(
            $$"""{"name":"Shape {{Guid.NewGuid()}}","totalSeats":4}""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/classes", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var names = doc.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray();
        string[] expected = ["availableSeats", "confirmedSeats", "heldSeats", "id", "name", "totalSeats"];
        Assert.Equal(expected, names);
    }

    private static async Task AssertErrorBodyAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(doc.RootElement.GetProperty("error").GetProperty("code").GetString()));
    }
}
