namespace SchoolAdmission.DataAccess;

public sealed class DataAccessOptions
{
    public const string SectionName = "DataAccess";

    public const string ConnectionStringName = "Default";

    /// <summary>Fixed key that serialises migrations across hosts.</summary>
    public const long MigrationLockKey = 7_319_204_551L;

    public const int MigrationMaxAttempts = 10;

    public static readonly TimeSpan MigrationRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Client command timeout. It is longer than the 30 s lock_timeout, so Postgres answers 55P03 first,
    /// and the migration advisory lock can wait for another host.
    /// </summary>
    public const int CommandTimeoutSeconds = 60;

    /// <summary>Bound from DataAccess:LockTimeoutSeconds. Must be positive, because 0 turns lock_timeout off.</summary>
    public int LockTimeoutSeconds { get; set; } = 30;
}
