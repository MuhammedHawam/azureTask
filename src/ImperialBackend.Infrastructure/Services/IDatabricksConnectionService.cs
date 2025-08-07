using System.Data;

namespace ImperialBackend.Infrastructure.Services;

/// <summary>
/// Interface for Databricks connection service
/// </summary>
public interface IDatabricksConnectionService
{
    /// <summary>
    /// Creates a new database connection to Databricks
    /// </summary>
    /// <returns>A new database connection</returns>
    IDbConnection CreateConnection();

    /// <summary>
    /// Gets the full table name with catalog and schema
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <returns>Fully qualified table name</returns>
    string GetFullTableName(string tableName);
}