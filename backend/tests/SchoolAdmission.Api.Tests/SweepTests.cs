using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Api.Tests.Concurrency;
using SchoolAdmission.Api.Tests.Infrastructure;
using SchoolAdmission.Business.Holds;

namespace SchoolAdmission.Api.Tests;

// The Concurrency collection never runs in parallel with other tests, so no other test creates stale rows
// while these run. That makes an exact count of 0 on the second sweep a sound assertion.
[Collection(ConcurrencyCollection.Name)]
public class SweepTests
{
    private const int ClassSeats = 3;
    private static readonly TimeSpan SweepWait = TimeSpan.FromSeconds(10);

    private static Task<string> StoredStatusAsync(string holdId) =>
        Sql.ScalarAsync<string>("select status from holds where id = $1", Guid.Parse(holdId));

    private static async Task<int> SweepAsync(AppFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<HoldService>().ExpireStaleAsync(CancellationToken.None);
    }

    private static async Task<(string Expired, string Live)> CreateExpiredAndLiveAsync(AppFactory factory)
    {
        using var client = factory.CreateClient();
        var classId = await TestData.CreateClassAsync(client, ClassSeats);
        var expired = await TestData.CreateHoldAsync(client, classId, TestData.NewStudentId());
        var live = await TestData.CreateHoldAsync(client, classId, TestData.NewStudentId());
        await Sql.ExpireAsync(Guid.Parse(expired));
        return (expired, live);
    }

    [Fact]
    public async Task ExpireStaleAsync_stores_EXPIRED_for_past_holds_only()
    {
        using var factory = new AppFactory();
        var (expired, live) = await CreateExpiredAndLiveAsync(factory);
        Assert.Equal("HELD", await StoredStatusAsync(expired));

        var count = await SweepAsync(factory);

        Assert.True(count >= 1);
        Assert.Equal("EXPIRED", await StoredStatusAsync(expired));
        Assert.Equal("HELD", await StoredStatusAsync(live));
    }

    [Fact]
    public async Task ExpireStaleAsync_twice_is_harmless()
    {
        using var factory = new AppFactory();
        var (expired, live) = await CreateExpiredAndLiveAsync(factory);
        await SweepAsync(factory);

        var second = await SweepAsync(factory);

        Assert.Equal(0, second);
        Assert.Equal("EXPIRED", await StoredStatusAsync(expired));
        Assert.Equal("HELD", await StoredStatusAsync(live));
    }

    [Fact]
    public async Task Background_sweep_stores_EXPIRED_without_a_direct_call()
    {
        using var factory = new AppFactory().WithSetting("EXPIRY_SWEEP_SECONDS", "1");
        var (expired, live) = await CreateExpiredAndLiveAsync(factory);

        var swept = await Wait.UntilAsync(async () => await StoredStatusAsync(expired) == "EXPIRED", SweepWait);

        Assert.True(swept, $"Hold {expired} was not stored as EXPIRED within {SweepWait.TotalSeconds} s.");

        Assert.Equal("HELD", await StoredStatusAsync(live));
    }
}
