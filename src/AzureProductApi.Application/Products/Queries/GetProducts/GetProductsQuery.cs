using AzureProductApi.Application.Common.Models;
using AzureProductApi.Application.DTOs;
using MediatR;

namespace AzureProductApi.Application.Products.Queries.GetProducts;

/// <summary>
/// Query to get products with optional filtering and pagination
/// </summary>
public record GetProductsQuery : IRequest<Result<PagedResult<ProductDto>>>
{
    /// <summary>
    /// Gets the category filter (optional)
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the active status filter (optional)
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Gets the search term for name/description search (optional)
    /// </summary>
    public string? SearchTerm { get; init; }

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