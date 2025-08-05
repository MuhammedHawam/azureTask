using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Domain.Enums;
using MediatR;

namespace ImperialBackend.Application.Outlets.Queries.GetOutlets;

/// <summary>
/// Query to get outlets with filtering, sorting, and pagination
/// </summary>
public record GetOutletsQuery : IRequest<Result<PagedResult<OutletDto>>>
{
    /// <summary>
    /// Gets the tier filter (optional)
    /// </summary>
    public string? Tier { get; init; }

    /// <summary>
    /// Gets the chain type filter (optional)
    /// </summary>
    public ChainType? ChainType { get; init; }

    /// <summary>
    /// Gets the active status filter (optional)
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Gets the city filter (optional)
    /// </summary>
    public string? City { get; init; }

    /// <summary>
    /// Gets the state filter (optional)
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Gets the search term for name/address search (optional)
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Gets the minimum rank filter (optional)
    /// </summary>
    public int? MinRank { get; init; }

    /// <summary>
    /// Gets the maximum rank filter (optional)
    /// </summary>
    public int? MaxRank { get; init; }

    /// <summary>
    /// Gets the filter for outlets needing visits (optional)
    /// </summary>
    public bool? NeedsVisit { get; init; }

    /// <summary>
    /// Gets the maximum days since visit for filtering outlets that need visits
    /// </summary>
    public int MaxDaysSinceVisit { get; init; } = 30;

    /// <summary>
    /// Gets the filter for high-performing outlets (optional)
    /// </summary>
    public bool? HighPerforming { get; init; }

    /// <summary>
    /// Gets the minimum achievement percentage for high-performing outlets
    /// </summary>
    public decimal MinAchievementPercentage { get; init; } = 80.0m;

    /// <summary>
    /// Gets the page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets the page size
    /// </summary>
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// Gets the sort field
    /// </summary>
    public string SortBy { get; init; } = "CreatedAt";

    /// <summary>
    /// Gets the sort direction (asc/desc)
    /// </summary>
    public string SortDirection { get; init; } = "desc";
}