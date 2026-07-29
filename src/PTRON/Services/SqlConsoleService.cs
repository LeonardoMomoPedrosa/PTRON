using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using PTRON.Data;

namespace PTRON.Services;

public class SqlConsoleService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public SqlConsoleService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<SqlConsoleResult> ExecuteAsync(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new InvalidOperationException("Informe um comando SQL.");
        }

        await using var db = await _factory.CreateDbContextAsync();
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql.Trim();

        if (IsQuery(command.CommandText))
        {
            return await ExecuteQueryAsync(command);
        }

        var affected = await command.ExecuteNonQueryAsync();
        return new SqlConsoleResult
        {
            IsQuery = false,
            RowsAffected = affected
        };
    }

    public async Task<List<string>> GetTableNamesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
              AND name NOT LIKE '__EF%'
            ORDER BY name
            """;

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static bool IsQuery(string sql)
    {
        var start = sql.TrimStart();
        return start.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
               || start.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)
               || start.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase)
               || start.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<SqlConsoleResult> ExecuteQueryAsync(DbCommand command)
    {
        await using var reader = await command.ExecuteReaderAsync();
        var columns = Enumerable.Range(0, reader.FieldCount)
            .Select(reader.GetName)
            .ToList();

        var rows = new List<IReadOnlyList<object?>>();
        while (await reader.ReadAsync())
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        return new SqlConsoleResult
        {
            IsQuery = true,
            Columns = columns,
            Rows = rows,
            RowsAffected = rows.Count
        };
    }
}

public class SqlConsoleResult
{
    public bool IsQuery { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyList<object?>> Rows { get; init; } = Array.Empty<IReadOnlyList<object?>>();
    public int RowsAffected { get; init; }
}
