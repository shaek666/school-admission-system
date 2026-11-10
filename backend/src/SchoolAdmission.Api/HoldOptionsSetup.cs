using Microsoft.Extensions.Options;
using SchoolAdmission.Business.Holds;

namespace SchoolAdmission.Api;

/// <summary>Reads the two root env keys and rejects anything that is not a positive integer.</summary>
public sealed class HoldOptionsSetup(IConfiguration configuration)
    : IConfigureOptions<HoldOptions>, IValidateOptions<HoldOptions>
{
    public const string DurationKey = "HOLD_DURATION_SECONDS";
    public const string SweepKey = "EXPIRY_SWEEP_SECONDS";

    public void Configure(HoldOptions options)
    {
        options.DurationSeconds = ReadOrZero(DurationKey, HoldOptions.DefaultDurationSeconds);
        options.SweepSeconds = ReadOrZero(SweepKey, HoldOptions.DefaultSweepSeconds);
    }

    public ValidateOptionsResult Validate(string? name, HoldOptions options)
    {
        var failures = new List<string>();
        if (options.DurationSeconds <= 0)
        {
            failures.Add(Describe(DurationKey));
        }

        if (options.SweepSeconds <= 0)
        {
            failures.Add(Describe(SweepKey));
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    // An unparsable value becomes 0, which Validate then rejects with the key name.
    private int ReadOrZero(string key, int fallback)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        return int.TryParse(raw, out var value) ? value : 0;
    }

    private string Describe(string key) =>
        $"{key} must be a positive integer, but the value is '{configuration[key]}'.";
}
