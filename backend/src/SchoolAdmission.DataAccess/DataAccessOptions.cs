namespace SchoolAdmission.DataAccess;

public sealed class DataAccessOptions
{
    public const string SectionName = "DataAccess";

    public const string ConnectionStringName = "Default";

    /// <summary>Fixed key that serialises migrations across hosts.</summary>
    public const long MigrationLockKey = 7_319_204_551L;

    public const int MigrationMaxAttempts = 10;

    public static readonly TimeSpan MigrationRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>Bound from DataAccess:LockTimeoutSeconds.</summary>
    public int LockTimeoutSeconds { get; set; } = 30;
}
