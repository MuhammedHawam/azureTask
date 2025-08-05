using AzureProductApi.Domain.Entities;

namespace AzureProductApi.Domain.Interfaces;

/// <summary>
/// Repository interface for Product entity operations
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Gets a product by its unique identifier
    /// </summary>
    /// <param name="id">The product identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The product if found, null otherwise</returns>
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a product by its SKU
    /// </summary>
    /// <param name="sku">The product SKU</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The product if found, null otherwise</returns>
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products with optional filtering and pagination
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of products</returns>
    Task<IEnumerable<Product>> GetAllAsync(
        string? category = null,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of products with optional filtering
    /// </summary>
    /// <param name="category">Optional category filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The total count of products</returns>
    Task<int> GetCountAsync(
        string? category = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches products by name or description
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of products matching the search term</returns>
    Task<IEnumerable<Product>> SearchAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct categories
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of categories</returns>
    Task<IEnumerable<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new product
    /// </summary>
    /// <param name="product">The product to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added product</returns>
    Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing product
    /// </summary>
    /// <param name="product">The product to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated product</returns>
    Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a product
    /// </summary>
    /// <param name="id">The product identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the product was deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a product with the given SKU exists
    /// </summary>
    /// <param name="sku">The product SKU</param>
    /// <param name="excludeId">Optional product ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if a product with the SKU exists, false otherwise</returns>
    Task<bool> ExistsWithSkuAsync(string sku, Guid? excludeId = null, CancellationToken cancellationToken = default);
}