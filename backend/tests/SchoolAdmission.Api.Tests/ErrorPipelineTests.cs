using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SchoolAdmission.Api.Http;
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
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal("NOT_FOUND", error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Preflight_from_127_0_0_1_allows_delete_and_header()
    {
        using var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/holds/x");
        request.Headers.Add("Origin", "http://127.0.0.1:3000");
        request.Headers.Add("Access-Control-Request-Method", "DELETE");
        request.Headers.Add("Access-Control-Request-Headers", StudentIdHeader.Name);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("*", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        var methods = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Methods"));
        var headers = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers"));
        Assert.Contains("DELETE", methods, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(StudentIdHeader.Name, headers, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void Factory_runs_in_production_like_the_container()
    {
        var environment = fixture.Factory.Services.GetRequiredService<IHostEnvironment>();

        Assert.True(environment.IsProduction());
    }

    [Fact]
    public void Route_handler_throws_on_bad_request_in_every_environment()
    {
        var options = fixture.Factory.Services.GetRequiredService<IOptions<RouteHandlerOptions>>().Value;

        Assert.True(options.ThrowOnBadRequest);
    }
}
