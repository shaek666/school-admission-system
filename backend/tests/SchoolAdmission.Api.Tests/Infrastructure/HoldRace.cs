using System.Net;

namespace SchoolAdmission.Api.Tests.Infrastructure;

/// <summary>Sends parallel hold requests for one class and collects each status and error code.</summary>
public static class HoldRace
{
    public const string HeldCountSql = "select count(*) from holds where class_id = $1 and status = 'HELD'";
    private static readonly TimeSpan RaceTimeout = TimeSpan.FromMinutes(3);

    /// <summary>Request i goes to clients[i % clients.Count], so several hosts share the load in turn.</summary>
    public static async Task<(HttpStatusCode Status, string? Code)[]> RunAsync(
        IReadOnlyList<HttpClient> clients, string classId, IReadOnlyList<string> studentIds)
    {
        var tasks = studentIds.Select(async (studentId, i) =>
        {
            using var response = await TestData.SendAsync(
                clients[i % clients.Count], HttpMethod.Post, $"/classes/{classId}/holds", studentId);
            var code = response.IsSuccessStatusCode ? null : await HttpJson.ErrorCodeAsync(response);
            return (response.StatusCode, code);
        }).ToArray();

        return await Task.WhenAll(tasks).WaitAsync(RaceTimeout);
    }

    public static List<string> NewStudents(int count) =>
        Enumerable.Range(0, count).Select(_ => TestData.NewStudentId()).ToList();
}
