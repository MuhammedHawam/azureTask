using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using MediatR;

namespace ImperialBackend.Application.Outlets.Queries.GetOutlets;

/// <summary>
/// Query to get outlets with pagination
/// </summary>
public record GetOutletsQuery : IRequest<Result<PagedResult<OutletDto>>>
{
    /// <summary>
    /// Gets the page number (1-based)
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Gets the page size
    /// </summary>
    public int PageSize { get; init; } = 10;
}