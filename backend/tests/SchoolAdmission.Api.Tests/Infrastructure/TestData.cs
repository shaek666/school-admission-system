using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Api.Http;

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

    /// <summary>Sends one request. The student id header is added only when <paramref name="studentId"/> is not null.</summary>
    public static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, string? studentId = null)
    {
        using var request = new HttpRequestMessage(method, url);
        if (studentId is not null)
        {
            request.Headers.Add(StudentIdHeader.Name, studentId);
        }

        return await client.SendAsync(request);
    }

    /// <summary>Creates a hold, asserts 201 and returns the response body.</summary>
    public static async Task<JsonElement> CreateHoldJsonAsync(HttpClient client, string classId, string studentId)
    {
        var response = await SendAsync(client, HttpMethod.Post, $"/classes/{classId}/holds", studentId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await HttpJson.ReadJsonAsync(response);
    }

    public static async Task<string> CreateHoldAsync(HttpClient client, string classId, string studentId) =>
        (await CreateHoldJsonAsync(client, classId, studentId)).GetProperty("holdId").GetString()!;
}
