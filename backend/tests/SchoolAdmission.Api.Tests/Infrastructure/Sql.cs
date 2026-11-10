using Npgsql;

namespace SchoolAdmission.Api.Tests.Infrastructure;

/// <summary>Raw SQL for tests. Arguments bind to $1, $2, ... in order. Each call owns and disposes its connection.</summary>
public static class Sql
{
    public static async Task ExecAsync(string sql, params object[] args)
    {
        await using var connection = new NpgsqlConnection(AppFactory.ConnectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, sql, args);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<T> ScalarAsync<T>(string sql, params object[] args)
    {
        await using var connection = new NpgsqlConnection(AppFactory.ConnectionString);
        await connection.OpenAsync();
        await using var command = CreateCommand(connection, sql, args);
        var value = await command.ExecuteScalarAsync();
        return (T)value!;
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, object[] args)
    {
        var command = new NpgsqlCommand(sql, connection);
        foreach (var arg in args)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = arg });
        }

        return command;
    }
}
