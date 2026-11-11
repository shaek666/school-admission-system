using Npgsql;

namespace SchoolAdmission.Api.Tests.Infrastructure;

/// <summary>Raw SQL for tests. Arguments bind to $1, $2, ... in order. Each command opens and closes its own connection.</summary>
public static class Sql
{
    private static readonly NpgsqlDataSource DataSource = NpgsqlDataSource.Create(AppFactory.ConnectionString);

    public static async Task ExecAsync(string sql, params object[] args)
    {
        await using var command = CreateCommand(sql, args);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Moves a hold's expires_at one second into the past, using database time.</summary>
    public static Task ExpireAsync(Guid holdId) =>
        ExecAsync("update holds set expires_at = now() - interval '1 second' where id = $1", holdId);

    public static async Task<T> ScalarAsync<T>(string sql, params object[] args)
    {
        await using var command = CreateCommand(sql, args);
        var value = await command.ExecuteScalarAsync();
        return (T)value!;
    }

    private static NpgsqlCommand CreateCommand(string sql, object[] args)
    {
        var command = DataSource.CreateCommand(sql);
        foreach (var arg in args)
        {
            command.Parameters.AddWithValue(arg);
        }

        return command;
    }
}
