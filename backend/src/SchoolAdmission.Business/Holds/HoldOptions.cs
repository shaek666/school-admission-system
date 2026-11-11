namespace SchoolAdmission.Business.Holds;

public sealed class HoldOptions
{
    public const int DefaultDurationSeconds = 600;
    public const int DefaultSweepSeconds = 30;

    /// <summary>One day. A longer sweep period is pointless, and PeriodicTimer rejects periods over about 49 days.</summary>
    public const int MaxSweepSeconds = 86_400;

    public int DurationSeconds { get; set; } = DefaultDurationSeconds;

    public int SweepSeconds { get; set; } = DefaultSweepSeconds;
}
