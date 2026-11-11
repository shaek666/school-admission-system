using System.Net;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests.Concurrency;

[Collection(ConcurrencyCollection.Name)]
public class TwoHostTests
{
    private const int Seats = 10;
    private const int Attempts = 200;
    private static readonly TimeSpan RaceTimeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task Two_hosts_on_one_database_never_oversell()
    {
        // Both factories read the same connection string, so both hosts use one database.
        using var hostA = new AppFactory();
        using var hostB = new AppFactory();
        using var clientA = hostA.CreateClient();
        using var clientB = hostB.CreateClient();
        var classId = await TestData.CreateClassAsync(clientA, Seats);

        var tasks = Enumerable.Range(0, Attempts).Select(async i =>
        {
            var client = i % 2 == 0 ? clientA : clientB;
            using var response = await TestData.SendAsync(
                client, HttpMethod.Post, $"/classes/{classId}/holds", TestData.NewStudentId());
            var code = response.IsSuccessStatusCode ? null : await HttpJson.ErrorCodeAsync(response);
            return (response.StatusCode, code);
        }).ToArray();
        var results = await Task.WhenAll(tasks).WaitAsync(RaceTimeout);

        Assert.Equal(Seats, results.Count(r => r.StatusCode == HttpStatusCode.Created));
        var rejected = results.Where(r => r.StatusCode != HttpStatusCode.Created).ToList();
        Assert.Equal(Attempts - Seats, rejected.Count);
        Assert.All(rejected, r =>
        {
            Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
            Assert.Equal("NO_SEATS_AVAILABLE", r.code);
        });

        foreach (var client in new[] { clientA, clientB })
        {
            using var response = await client.GetAsync($"/classes/{classId}");
            var body = await HttpJson.ReadJsonAsync(response);
            Assert.Equal(Seats, body.GetProperty("heldSeats").GetInt32());
            Assert.Equal(0, body.GetProperty("availableSeats").GetInt32());
        }

        Assert.Equal(
            (long)Seats,
            await Sql.ScalarAsync<long>(
                "select count(*) from holds where class_id = $1 and status = 'HELD'", Guid.Parse(classId)));
    }
}
