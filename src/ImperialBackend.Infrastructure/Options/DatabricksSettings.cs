namespace ImperialBackend.Infrastructure.Options;

public class DatabricksSettings
{
    public string? ServerHostname { get; set; }
    public string? HTTPPath { get; set; }
    public string? AccessToken { get; set; }
    public string? Catalog { get; set; }
    public string? Schema { get; set; }
    public string? OutletsTable { get; set; }
}