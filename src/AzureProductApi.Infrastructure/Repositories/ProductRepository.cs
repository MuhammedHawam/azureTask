using AzureProductApi.Domain.Entities;
using AzureProductApi.Domain.Interfaces;
using AzureProductApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AzureProductApi.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IProductRepository
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the ProductRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger</param>
    public ProductRepository(ApplicationDbContext context, ILogger<ProductRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting product by ID: {ProductId}", id);
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting product by SKU: {SKU}", sku);
        return await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> GetAllAsync(
        string? category = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting products - Category: {Category}, IsActive: {IsActive}, Page: {PageNumber}, PageSize: {PageSize}",
            category, isActive, pageNumber, pageSize);

        var query = _context.Products.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        // Order by creation date (most recent first)
        query = query.OrderByDescending(p => p.CreatedAt);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(
        string? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting product count - Category: {Category}, IsActive: {IsActive}", category, isActive);

        var query = _context.Products.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category.ToLower() == category.ToLower());
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Product>> SearchAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching products with term: {SearchTerm}, Page: {PageNumber}, PageSize: {PageSize}",
            searchTerm, pageNumber, pageSize);

        var query = _context.Products
            .Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
            .OrderByDescending(p => p.CreatedAt);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all product categories");
        return await _context.Products
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding product with SKU: {SKU}", product.SKU);
        
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Successfully added product with ID: {ProductId}", product.Id);
        return product;
    }

    /// <inheritdoc />
    public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating product with ID: {ProductId}", product.Id);
        
        _context.Products.Update(product);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Successfully updated product with ID: {ProductId}", product.Id);
        return product;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting product with ID: {ProductId}", id);
        
        var product = await _context.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found for deletion", id);
            return false;
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Successfully deleted product with ID: {ProductId}", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithSkuAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if product exists with SKU: {SKU}, ExcludeId: {ExcludeId}", sku, excludeId);
        
        var query = _context.Products.Where(p => p.SKU == sku);
        
        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}