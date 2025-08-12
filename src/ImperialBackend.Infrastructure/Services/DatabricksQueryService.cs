using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using ImperialBackend.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Infrastructure.Services;

public class DatabricksQueryService : IDatabricksQueryService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabricksQueryService> _logger;

    public DatabricksQueryService(IConfiguration configuration, ILogger<DatabricksQueryService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Databricks connection string 'DefaultConnection' is not configured.");
        _logger = logger;
    }

    public async Task<IReadOnlyList<IDictionary<string, object?>>> QueryAsync(GenericQuerySpec querySpec, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(querySpec.Catalog) || string.IsNullOrWhiteSpace(querySpec.Schema) || string.IsNullOrWhiteSpace(querySpec.Table))
        {
            throw new ArgumentException("Catalog, Schema and Table are required.");
        }

        var sql = BuildSql(querySpec);
        _logger.LogInformation("Executing Databricks generic query: {Sql}", sql);

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        using var command = new SqlCommand(sql, connection) { CommandType = CommandType.Text };
        command.CommandTimeout = 120;

        var results = new List<IDictionary<string, object?>>();
        using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var value = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
                row[reader.GetName(i)] = value;
            }
            results.Add(row);
        }

        return results;
    }

    private static string BuildSql(GenericQuerySpec querySpec)
    {
        var columnsPart = (querySpec.Columns != null && querySpec.Columns.Count > 0)
            ? string.Join(", ", querySpec.Columns.Select(c => QuoteIdentifier(c)))
            : "*";

        var fullTable = string.Join('.', new[] { querySpec.Catalog, querySpec.Schema, querySpec.Table }.Select(QuoteIdentifier));
        var sb = new StringBuilder();
        sb.Append($"SELECT {columnsPart} FROM {fullTable}");

        if (querySpec.EqualsFilters != null && querySpec.EqualsFilters.Count > 0)
        {
            var predicates = new List<string>();
            foreach (var kvp in querySpec.EqualsFilters)
            {
                var col = QuoteIdentifier(kvp.Key);
                predicates.Add(FormatLiteralEquals(col, kvp.Value));
            }
            sb.Append(" WHERE ");
            sb.Append(string.Join(" AND ", predicates));
        }

        if (querySpec.OrderBy != null && querySpec.OrderBy.Count > 0)
        {
            var orderParts = querySpec.OrderBy.Select(o => $"{QuoteIdentifier(o.Column)} {(o.Direction?.Equals("desc", StringComparison.OrdinalIgnoreCase) == true ? "DESC" : "ASC")}");
            sb.Append(" ORDER BY ");
            sb.Append(string.Join(", ", orderParts));
        }

        if (querySpec.Offset.HasValue || querySpec.Limit.HasValue)
        {
            var offset = querySpec.Offset.GetValueOrDefault(0);
            var limit = querySpec.Limit ?? int.MaxValue;
            sb.Append($" OFFSET {offset} ROWS FETCH NEXT {limit} ROWS ONLY");
        }

        return sb.ToString();
    }

    private static string QuoteIdentifier(string identifier)
    {
        if (identifier.Contains('[') || identifier.Contains(']'))
        {
            throw new ArgumentException("Invalid identifier.");
        }
        return $"[{identifier}]";
    }

    private static string FormatLiteralEquals(string quotedColumn, object? value)
    {
        if (value is null)
        {
            return $"{quotedColumn} IS NULL";
        }

        return value switch
        {
            string s => $"{quotedColumn} = '{EscapeLiteral(s)}'",
            bool b => $"{quotedColumn} = {(b ? 1 : 0)}",
            DateTime dt => $"{quotedColumn} = '{dt:yyyy-MM-dd HH:mm:ss}'",
            Guid g => $"{quotedColumn} = '{g}'",
            IFormattable f => $"{quotedColumn} = {f.ToString(null, System.Globalization.CultureInfo.InvariantCulture)}",
            _ => $"{quotedColumn} = '{EscapeLiteral(value.ToString() ?? string.Empty)}'"
        };
    }

    private static string EscapeLiteral(string input) => input.Replace("'", "''");
}