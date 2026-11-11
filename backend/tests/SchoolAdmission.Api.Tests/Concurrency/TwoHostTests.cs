using System.Net;
using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests.Concurrency;

[Collection(ConcurrencyCollection.Name)]
public class TwoHostTests
{
    private const int Seats = 10;
    private const int Attempts = 200;

    [Fact]
    public async Task Two_hosts_on_one_database_never_oversell()
    {
        // Both factories read the static AppFactory.ConnectionString, so both hosts use one database.
        // They share one process and one Npgsql pool, so this covers the database lock only.
        using var hostA = new AppFactory();
        using var hostB = new AppFactory();
        using var clientA = hostA.CreateClient();
        using var clientB = hostB.CreateClient();
        var classId = await TestData.CreateClassAsync(clientA, Seats);

        // Requests alternate between the two hosts.
        var results = await HoldRace.RunAsync([clientA, clientB], classId, HoldRace.NewStudents(Attempts));

        Assert.Equal(Seats, results.Count(r => r.Status == HttpStatusCode.Created));
        var rejected = results.Where(r => r.Status != HttpStatusCode.Created).ToList();
        Assert.Equal(Attempts - Seats, rejected.Count);
        Assert.All(rejected, r =>
        {
            Assert.Equal(HttpStatusCode.Conflict, r.Status);
            Assert.Equal("NO_SEATS_AVAILABLE", r.Code);
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
            await Sql.ScalarAsync<long>(HoldRace.HeldCountSql, Guid.Parse(classId)));
    }
}
