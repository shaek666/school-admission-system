using System.Globalization;
using System.Net.Http.Json;

namespace SchoolAdmission.Api.Tests.Infrastructure;

public static class TestData
{
    private const int StudentIdCounterDigits = 8;
    private const int StudentIdPrefixDigits = 8;

    // A leading 9 plus a random per-run prefix keeps ids apart from fixed ids in other tests and from other runs.
    private static readonly string StudentIdPrefix =
        "9" + Random.Shared.Next(0, (int)Math.Pow(10, StudentIdPrefixDigits)).ToString("D" + StudentIdPrefixDigits, CultureInfo.InvariantCulture);

    private static long _studentIdCounter;

    /// <summary>Returns a unique 17 digit student id for each call.</summary>
    public static string NewStudentId() =>
        StudentIdPrefix + Interlocked.Increment(ref _studentIdCounter).ToString("D" + StudentIdCounterDigits, CultureInfo.InvariantCulture);

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
