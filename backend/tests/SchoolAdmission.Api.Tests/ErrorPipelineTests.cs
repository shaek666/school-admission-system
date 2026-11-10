using System.Net;
using System.Text.Json;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

[Collection(PostgresCollection.Name)]
public class ErrorPipelineTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Unknown_route_returns_404_with_error_body()
    {
        using var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var code = doc.RootElement.GetProperty("error").GetProperty("code").GetString();
        Assert.False(string.IsNullOrEmpty(code));
    }

    [Fact]
    public async Task Preflight_from_127_0_0_1_allows_delete_and_header()
    {
        using var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/holds/x");
        request.Headers.Add("Origin", "http://127.0.0.1:3000");
        request.Headers.Add("Access-Control-Request-Method", "DELETE");
        request.Headers.Add("Access-Control-Request-Headers", "X-Student-Id");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("*", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Responses_expose_date_header()
    {
        using var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/nope");
        request.Headers.Add("Origin", "http://127.0.0.1:3000");

        var response = await client.SendAsync(request);

        var exposed = string.Join(",", response.Headers.GetValues("Access-Control-Expose-Headers"));
        Assert.Contains("Date", exposed);
    }
}
