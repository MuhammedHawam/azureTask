namespace ImperialBackend.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Azure Databricks connection
/// </summary>
public class DatabricksOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Databricks";

    /// <summary>
    /// Gets or sets the Databricks server hostname
    /// </summary>
    public string ServerHostname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP path for the SQL warehouse
    /// </summary>
    public string HTTPPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the access token for authentication
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the catalog name
    /// </summary>
    public string Catalog { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schema name
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full connection string for ODBC
    /// </summary>
    public string GetConnectionString()
    {
        return $"Driver={{Simba Spark ODBC Driver}};Host={ServerHostname};Port=443;HTTPPath={HTTPPath};SSL=1;ThriftTransport=2;AuthMech=3;UID=token;PWD={AccessToken};";
    }

    /// <summary>
    /// Gets the full table name with catalog and schema
    /// </summary>
    /// <param name="tableName">The table name</param>
    /// <returns>Fully qualified table name</returns>
    public string GetFullTableName(string tableName)
    {
        return $"{Catalog}.{Schema}.{tableName}";
    }
}