using AutoMapper;
using AzureProductApi.Application.Common.Models;
using AzureProductApi.Application.DTOs;
using AzureProductApi.Domain.Entities;
using AzureProductApi.Domain.Interfaces;
using AzureProductApi.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AzureProductApi.Application.Products.Commands.CreateProduct;

/// <summary>
/// Handler for CreateProductCommand
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CreateProductCommandHandler class
    /// </summary>
    /// <param name="productRepository">The product repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="logger">The logger</param>
    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IMapper mapper,
        ILogger<CreateProductCommandHandler> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the CreateProductCommand
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the created product or error information</returns>
    public async Task<Result<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating product with SKU: {SKU}", request.SKU);

            // Check if product with same SKU already exists
            var existingProduct = await _productRepository.GetBySkuAsync(request.SKU, cancellationToken);
            if (existingProduct != null)
            {
                _logger.LogWarning("Product with SKU {SKU} already exists", request.SKU);
                return Result<ProductDto>.Failure($"Product with SKU '{request.SKU}' already exists");
            }

            // Create the Money value object
            var price = new Money(request.Price, request.Currency);

            // Create the product entity
            var product = new Product(
                request.Name,
                request.Description,
                price,
                request.Category,
                request.SKU);

            // Set additional properties
            product.UpdateStock(request.StockQuantity, request.UserId);
            product.SetCreationInfo(request.UserId);

            // Add tags
            foreach (var tag in request.Tags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                product.AddTag(tag.Trim(), request.UserId);
            }

            // Set image URL if provided
            if (!string.IsNullOrEmpty(request.ImageUrl))
            {
                product.SetImageUrl(request.ImageUrl, request.UserId);
            }

            // Save to repository
            var createdProduct = await _productRepository.AddAsync(product, cancellationToken);

            // Map to DTO
            var productDto = _mapper.Map<ProductDto>(createdProduct);

            _logger.LogInformation("Successfully created product with ID: {ProductId}", createdProduct.Id);

            return Result<ProductDto>.Success(productDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating product: {Message}", ex.Message);
            return Result<ProductDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product with SKU: {SKU}", request.SKU);
            return Result<ProductDto>.Failure("An error occurred while creating the product");
        }
    }
}