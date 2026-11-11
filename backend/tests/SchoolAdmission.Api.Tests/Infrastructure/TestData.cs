using System.Net.Http.Json;

namespace SchoolAdmission.Api.Tests.Infrastructure;

public static class TestData
{
    public static Task<HttpResponseMessage> PostClassAsync(HttpClient client, int seats, string? name = null) =>
        client.PostAsJsonAsync("/classes", new
        {
            name = name ?? "Class 6 - Morning Shift " + Guid.NewGuid(),
            totalSeats = seats,
        });

    public static async Task<string> CreateClassAsync(HttpClient client, int seats, string? name = null)
    {
        var response = await PostClassAsync(client, seats, name);
        response.EnsureSuccessStatusCode();
        return (await HttpJson.ReadJsonAsync(response)).GetProperty("id").GetString()!;
    }
}
