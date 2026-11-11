namespace SchoolAdmission.Api.Tests.Infrastructure;

public static class Wait
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Polls until the condition is true (returns true) or the timeout passes (returns false).
    /// The token passed to the condition is cancelled at the deadline, so one slow call cannot outlive the timeout.
    /// </summary>
    public static async Task<bool> UntilAsync(
        Func<CancellationToken, Task<bool>> condition, TimeSpan timeout, TimeSpan? interval = null)
    {
        using var deadline = new CancellationTokenSource(timeout);
        try
        {
            while (!await condition(deadline.Token))
            {
                await Task.Delay(interval ?? DefaultInterval, deadline.Token);
            }

            return true;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return false;
        }
    }
}
