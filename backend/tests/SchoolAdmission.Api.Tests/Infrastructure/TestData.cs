using System.Net.Http.Json;
using System.Text.Json;

namespace SchoolAdmission.Api.Tests.Infrastructure;

public static class TestData
{
    public static async Task<string> CreateClassAsync(HttpClient client, int seats, string? name = null)
    {
        var response = await client.PostAsJsonAsync("/classes", new
        {
            name = name ?? "Class 6 - Morning Shift " + Guid.NewGuid(),
            totalSeats = seats,
        });
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetString()!;
    }
}
