using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace SchoolAdmission.DataAccess;

/// <summary>
/// Creates the database if it is missing, then applies migrations under a Postgres advisory lock,
/// because EF Core 8 does not lock migrations and two hosts that start together can race.
/// </summary>
public sealed class DatabaseMigrator
{
    private const string MaintenanceDatabase = "postgres";
    private const string DuplicateDatabaseSqlState = PostgresErrorCodes.DuplicateDatabase;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseMigrator> _logger;

    public DatabaseMigrator(IServiceScopeFactory scopeFactory, ILogger<DatabaseMigrator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await MigrateOnceAsync(cancellationToken);
                return;
            }
            catch (Exception ex) when (
                !cancellationToken.IsCancellationRequested
                && attempt < DataAccessOptions.MigrationMaxAttempts)
            {
                _logger.LogWarning(
                    ex,
                    "Database migration attempt {Attempt} of {MaxAttempts} failed. Retrying.",
                    attempt,
                    DataAccessOptions.MigrationMaxAttempts);
                await Task.Delay(DataAccessOptions.MigrationRetryDelay, cancellationToken);
            }
        }
    }

    private async Task MigrateOnceAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
        var connectionString = context.Database.GetConnectionString()
            ?? throw new InvalidOperationException("The DbContext has no connection string.");

        await EnsureDatabaseExistsAsync(connectionString, cancellationToken);

        // OpenConnectionAsync keeps one connection open, so the lock, the migration and the unlock share a session.
        await context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await context.Database.ExecuteSqlAsync(
                $"select pg_advisory_lock({DataAccessOptions.MigrationLockKey})", cancellationToken);
            try
            {
                await context.Database.MigrateAsync(cancellationToken);
            }
            finally
            {
                await UnlockAsync(context);
            }
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    private async Task UnlockAsync(AdmissionDbContext context)
    {
        try
        {
            await context.Database.ExecuteSqlAsync(
                $"select pg_advisory_unlock({DataAccessOptions.MigrationLockKey})", CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Closing the connection also releases the lock, so a failed unlock is not fatal.
            _logger.LogWarning(ex, "Releasing the migration advisory lock failed.");
        }
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var target = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = target.Database
            ?? throw new InvalidOperationException("The connection string has no database name.");

        var maintenance = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = MaintenanceDatabase,
            Pooling = false,
        };

        await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var exists = new NpgsqlCommand("select 1 from pg_database where datname = $1", connection);
        exists.Parameters.Add(new NpgsqlParameter { Value = databaseName });
        if (await exists.ExecuteScalarAsync(cancellationToken) is not null)
        {
            return;
        }

        await using var create = new NpgsqlCommand($"create database {QuoteIdentifier(databaseName)}", connection);
        try
        {
            await create.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == DuplicateDatabaseSqlState)
        {
            // Another host created it between the check and the create. That is fine.
        }
    }

    private static string QuoteIdentifier(string identifier) =>
        "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
