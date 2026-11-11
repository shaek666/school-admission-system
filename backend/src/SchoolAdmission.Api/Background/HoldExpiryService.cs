using SchoolAdmission.Business.Holds;

namespace SchoolAdmission.Api.Background;

/// <summary>
/// Stores EXPIRED on stale HELD holds every <see cref="HoldOptions.SweepSeconds"/>. Expiry is already correct at read time,
/// so a failed tick costs nothing: it is logged and the next tick retries.
/// </summary>
public sealed class HoldExpiryService(
    IServiceScopeFactory scopeFactory,
    HoldOptions options,
    ILogger<HoldExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.SweepSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<HoldService>().ExpireStaleAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogWarning(ex, "Hold expiry sweep failed; retrying on the next tick");
                }
            }
        }
        catch (Exception) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown ends the loop quietly, whatever the provider throws while it is cancelled.
        }
    }
}
