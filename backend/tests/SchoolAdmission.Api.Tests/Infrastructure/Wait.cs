namespace SchoolAdmission.Api.Tests.Infrastructure;

public static class Wait
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(200);

    /// <summary>Polls until the condition is true (returns true) or the timeout passes (returns false).</summary>
    public static async Task<bool> UntilAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan? interval = null)
    {
        var delay = interval ?? DefaultInterval;
        var deadline = DateTime.UtcNow + timeout;
        while (true)
        {
            if (await condition())
            {
                return true;
            }

            if (DateTime.UtcNow >= deadline)
            {
                return false;
            }

            await Task.Delay(delay);
        }
    }
}
