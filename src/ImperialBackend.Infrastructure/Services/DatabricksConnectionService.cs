using ImperialBackend.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using System.Data;
using System.Data.Odbc;

namespace ImperialBackend.Infrastructure.Services;

/// <summary>
/// Service for managing Databricks database connections
/// </summary>
public class DatabricksConnectionService : IDatabricksConnectionService
{
    private readonly DatabricksOptions _options;

    /// <summary>
    /// Initializes a new instance of the DatabricksConnectionService class
    /// </summary>
    /// <param name="options">Databricks configuration options</param>
    public DatabricksConnectionService(IOptions<DatabricksOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public IDbConnection CreateConnection()
    {
        var connectionString = _options.GetConnectionString();
        return new OdbcConnection(connectionString);
    }

    /// <inheritdoc />
    public string GetFullTableName(string tableName)
    {
        return _options.GetFullTableName(tableName);
    }
}