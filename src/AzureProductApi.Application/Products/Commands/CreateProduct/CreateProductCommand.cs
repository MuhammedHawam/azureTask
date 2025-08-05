using AzureProductApi.Application.Common.Models;
using AzureProductApi.Application.DTOs;
using MediatR;

namespace AzureProductApi.Application.Products.Commands.CreateProduct;

/// <summary>
/// Command to create a new product
/// </summary>
public record CreateProductCommand : IRequest<Result<ProductDto>>
{
    /// <summary>
    /// Gets the product name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the product description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the product price amount
    /// </summary>
    public decimal Price { get; init; }

    /// <summary>
    /// Gets the product price currency
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Gets the product category
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets the product SKU
    /// </summary>
    public string SKU { get; init; } = string.Empty;

    /// <summary>
    /// Gets the initial stock quantity
    /// </summary>
    public int StockQuantity { get; init; }

    /// <summary>
    /// Gets the product tags
    /// </summary>
    public List<string> Tags { get; init; } = new();

    /// <summary>
    /// Gets the product image URL
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// Gets the user identifier who is creating the product
    /// </summary>
    public string UserId { get; init; } = string.Empty;
}