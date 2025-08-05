using AutoMapper;
using AzureProductApi.Application.Common.Models;
using AzureProductApi.Application.DTOs;
using AzureProductApi.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AzureProductApi.Application.Products.Queries.GetProducts;

/// <summary>
/// Handler for GetProductsQuery
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetProductsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the GetProductsQueryHandler class
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="logger">The logger</param>
    public GetProductsQueryHandler(
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<GetProductsQueryHandler> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the GetProductsQuery
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the paged products or error information</returns>
    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting products - Page: {PageNumber}, PageSize: {PageSize}, Category: {Category}, IsActive: {IsActive}, SearchTerm: {SearchTerm}",
                request.PageNumber, request.PageSize, request.Category, request.IsActive, request.SearchTerm);

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return Result<PagedResult<ProductDto>>.Failure("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<PagedResult<ProductDto>>.Failure("Page size must be between 1 and 100");
            }

            IEnumerable<Domain.Entities.Product> products;
            int totalCount;

            // Use search if search term is provided
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                products = await _productRepository.SearchAsync(
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                // For search, we need to get the total count differently
                // This is a simplified approach - in a real application, you might want to optimize this
                var allSearchResults = await _productRepository.SearchAsync(
                    request.SearchTerm,
                    1,
                    int.MaxValue,
                    cancellationToken);
                totalCount = allSearchResults.Count();
            }
            else
            {
                // Get products with filtering
                products = await _productRepository.GetAllAsync(
                    request.Category,
                    request.IsActive,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                // Get total count for pagination
                totalCount = await _productRepository.GetCountAsync(
                    request.Category,
                    request.IsActive,
                    cancellationToken);
            }

            // Map to DTOs
            var productDtos = _mapper.Map<List<ProductDto>>(products);

            // Apply sorting if needed (this could be moved to repository for better performance)
            productDtos = ApplySorting(productDtos, request.SortBy, request.SortDirection);

            // Create paged result
            var pagedResult = new PagedResult<ProductDto>(
                productDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation("Successfully retrieved {Count} products out of {TotalCount} total",
                pagedResult.Count, pagedResult.TotalCount);

            return Result<PagedResult<ProductDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return Result<PagedResult<ProductDto>>.Failure("An error occurred while retrieving products");
        }
    }

    private static List<ProductDto> ApplySorting(List<ProductDto> products, string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "name" => isDescending
                ? products.OrderByDescending(p => p.Name).ToList()
                : products.OrderBy(p => p.Name).ToList(),
            "price" => isDescending
                ? products.OrderByDescending(p => p.Price).ToList()
                : products.OrderBy(p => p.Price).ToList(),
            "category" => isDescending
                ? products.OrderByDescending(p => p.Category).ToList()
                : products.OrderBy(p => p.Category).ToList(),
            "sku" => isDescending
                ? products.OrderByDescending(p => p.SKU).ToList()
                : products.OrderBy(p => p.SKU).ToList(),
            "stockquantity" => isDescending
                ? products.OrderByDescending(p => p.StockQuantity).ToList()
                : products.OrderBy(p => p.StockQuantity).ToList(),
            "updatedat" => isDescending
                ? products.OrderByDescending(p => p.UpdatedAt).ToList()
                : products.OrderBy(p => p.UpdatedAt).ToList(),
            _ => isDescending
                ? products.OrderByDescending(p => p.CreatedAt).ToList()
                : products.OrderBy(p => p.CreatedAt).ToList()
        };
    }
}