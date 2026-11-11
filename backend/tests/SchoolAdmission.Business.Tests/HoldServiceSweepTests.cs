using Microsoft.Extensions.Logging.Abstractions;
using SchoolAdmission.Business.Holds;
using SchoolAdmission.Business.Tests.Fakes;

namespace SchoolAdmission.Business.Tests;

public class HoldServiceSweepTests
{
    private const int Swept = 4;

    [Fact]
    public async Task ExpireStaleAsync_returns_the_repository_count_without_a_transaction()
    {
        var log = new CallLog();
        var holds = new FakeHoldRepository(log) { ExpireAllCount = Swept };
        var service = new HoldService(
            new FakeUnitOfWork(log), new FakeClassRepository(log), holds,
            new HoldOptions(), NullLogger<HoldService>.Instance);

        var count = await service.ExpireStaleAsync(CancellationToken.None);

        Assert.Equal(Swept, count);
        Assert.Equal([CallLog.ExpireAll], log.Entries);
    }
}
