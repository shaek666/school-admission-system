using SchoolAdmission.Api.Tests.Infrastructure;

namespace SchoolAdmission.Api.Tests;

public class StartupTests
{
    [Theory]
    [InlineData("HOLD_DURATION_SECONDS", "0")]
    [InlineData("HOLD_DURATION_SECONDS", "-5")]
    [InlineData("HOLD_DURATION_SECONDS", "abc")]
    [InlineData("EXPIRY_SWEEP_SECONDS", "0")]
    [InlineData("EXPIRY_SWEEP_SECONDS", "abc")]
    [InlineData("EXPIRY_SWEEP_SECONDS", "86401")] // over the 1 day cap; PeriodicTimer itself throws past about 49 days
    public void Startup_fails_for_bad_setting(string key, string value)
    {
        using var factory = new AppFactory().WithSetting(key, value);

        var ex = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        Assert.Contains(key, ex.ToString());
    }
}
