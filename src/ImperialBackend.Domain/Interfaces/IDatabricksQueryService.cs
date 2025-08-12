namespace ImperialBackend.Domain.Interfaces;

public interface IDatabricksQueryService
{
    Task<IReadOnlyList<IDictionary<string, object?>>> QueryAsync(GenericQuerySpec querySpec, CancellationToken cancellationToken = default);
}

public class GenericQuerySpec
{
    public string Catalog { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
    public IReadOnlyList<string>? Columns { get; set; }
    public IReadOnlyDictionary<string, object?>? EqualsFilters { get; set; }
    public IReadOnlyList<SortSpec>? OrderBy { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

public class SortSpec
{
    public string Column { get; set; } = string.Empty;
    public string Direction { get; set; } = "asc"; // asc or desc
}