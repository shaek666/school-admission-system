namespace SchoolAdmission.DataAccess;

public static class DataAccessOptions
{
    public const string ConnectionStringName = "Default";

    /// <summary>Fixed key that serialises migrations across hosts.</summary>
    public const long MigrationLockKey = 7_319_204_551L;

    public const int MigrationMaxAttempts = 10;

    public static readonly TimeSpan MigrationRetryDelay = TimeSpan.FromSeconds(2);
}
