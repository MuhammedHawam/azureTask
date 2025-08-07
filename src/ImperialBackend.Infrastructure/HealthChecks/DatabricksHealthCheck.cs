using ImperialBackend.Infrastructure.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Databricks connectivity
/// </summary>
public class DatabricksHealthCheck : IHealthCheck
{
    private readonly IDatabricksConnectionService _connectionService;
    private readonly ILogger<DatabricksHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the DatabricksHealthCheck class
    /// </summary>
    /// <param name="connectionService">The Databricks connection service</param>
    /// <param name="logger">The logger</param>
    public DatabricksHealthCheck(IDatabricksConnectionService connectionService, ILogger<DatabricksHealthCheck> logger)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Databricks connectivity");

            using var connection = _connectionService.CreateConnection();
            connection.Open();

            // Simple query to test connectivity
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            var result = await Task.Run(() => command.ExecuteScalar(), cancellationToken);

            if (result != null && result.ToString() == "1")
            {
                _logger.LogDebug("Databricks health check passed");
                return HealthCheckResult.Healthy("Databricks connection is healthy");
            }

            _logger.LogWarning("Databricks health check failed: Unexpected query result");
            return HealthCheckResult.Unhealthy("Databricks connection returned unexpected result");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Databricks health check failed");
            return HealthCheckResult.Unhealthy($"Databricks connection failed: {ex.Message}", ex);
        }
    }
}