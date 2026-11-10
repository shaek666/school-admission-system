namespace SchoolAdmission.Business.Holds;

public sealed class HoldOptions
{
    public const int DefaultDurationSeconds = 600;
    public const int DefaultSweepSeconds = 30;

    public int DurationSeconds { get; set; } = DefaultDurationSeconds;

    public int SweepSeconds { get; set; } = DefaultSweepSeconds;
}
