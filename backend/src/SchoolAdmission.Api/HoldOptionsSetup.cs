using SchoolAdmission.Business.Holds;

namespace SchoolAdmission.Api;

public static class HoldOptionsSetup
{
    public const string DurationKey = "HOLD_DURATION_SECONDS";
    public const string SweepKey = "EXPIRY_SWEEP_SECONDS";

    /// <summary>Reads the two root env keys and rejects anything that is not a positive integer.</summary>
    public static IServiceCollection AddHoldOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<HoldOptions>()
            .Configure(options =>
            {
                options.DurationSeconds = ReadOrZero(configuration, DurationKey, HoldOptions.DefaultDurationSeconds);
                options.SweepSeconds = ReadOrZero(configuration, SweepKey, HoldOptions.DefaultSweepSeconds);
            })
            .Validate(options => options.DurationSeconds > 0, Describe(configuration, DurationKey))
            .Validate(options => options.SweepSeconds > 0, Describe(configuration, SweepKey))
            .ValidateOnStart();
        return services;
    }

    // An unparsable value becomes 0, which the validators then reject with the key name.
    private static int ReadOrZero(IConfiguration configuration, string key, int fallback)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return int.TryParse(raw, out var value) ? value : 0;
    }

    private static string Describe(IConfiguration configuration, string key) =>
        $"{key} must be a positive integer, but the value is '{configuration[key]}'.";
}
