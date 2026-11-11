using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolAdmission.Api.Background;
using SchoolAdmission.Api.Tests.Infrastructure;
using SchoolAdmission.Business;
using SchoolAdmission.Business.Classes;
using SchoolAdmission.Business.Holds;

namespace SchoolAdmission.Api.Tests.Background;

public class HoldExpiryServiceTests
{
    private static readonly TimeSpan LoopWait = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task A_failed_tick_is_logged_and_the_loop_keeps_running()
    {
        // The first sweep throws, as a transient database error would. The loop must survive it and tick again.
        var calls = 0;
        var holds = Stub.Create<IHoldRepository>((method, _) =>
        {
            if (method.Name != nameof(IHoldRepository.ExpireAllStaleAsync))
            {
                throw new NotSupportedException(method.Name);
            }

            return Interlocked.Increment(ref calls) == 1
                ? Task.FromException<int>(new InvalidOperationException("transient"))
                : Task.FromResult(0);
        });
        var options = new HoldOptions { SweepSeconds = 1 };
        await using var provider = new ServiceCollection()
            .AddSingleton(options)
            .AddSingleton(holds)
            .AddSingleton(Stub.Create<IUnitOfWork>((method, _) => throw new NotSupportedException(method.Name)))
            .AddSingleton(Stub.Create<IClassRepository>((method, _) => throw new NotSupportedException(method.Name)))
            .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>))
            .AddScoped<HoldService>()
            .BuildServiceProvider();
        var service = new HoldExpiryService(
            provider.GetRequiredService<IServiceScopeFactory>(), options, NullLogger<HoldExpiryService>.Instance);

        await service.StartAsync(CancellationToken.None);
        var tickedAgain = await Wait.UntilAsync(_ => Task.FromResult(Volatile.Read(ref calls) >= 2), LoopWait);
        await service.StopAsync(CancellationToken.None);

        Assert.True(tickedAgain, $"The sweep ran {Volatile.Read(ref calls)} time(s); the loop stopped after the failed tick.");
        Assert.False(service.ExecuteTask!.IsFaulted);
    }

    /// <summary>An interface stub whose every call goes to one handler.</summary>
    public class Stub : DispatchProxy
    {
        private Func<MethodInfo, object?[]?, object?> _handler = (method, _) => throw new NotSupportedException(method.Name);

        public static T Create<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
        {
            var proxy = Create<T, Stub>();
            ((Stub)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => _handler(targetMethod!, args);
    }
}
