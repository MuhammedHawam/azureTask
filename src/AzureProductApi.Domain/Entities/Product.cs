using AzureProductApi.Domain.Common;
using AzureProductApi.Domain.ValueObjects;

namespace AzureProductApi.Domain.Entities;

/// <summary>
/// Represents a product entity with business rules and invariants
/// </summary>
public class Product : BaseEntity
{
    private readonly List<string> _tags = new();

    /// <summary>
    /// Initializes a new instance of the Product class
    /// </summary>
    /// <param name="name">The product name</param>
    /// <param name="description">The product description</param>
    /// <param name="price">The product price</param>
    /// <param name="category">The product category</param>
    /// <param name="sku">The product SKU</param>
    public Product(string name, string description, Money price, string category, string sku)
    {
        Name = ValidateName(name);
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Category = ValidateCategory(category);
        SKU = ValidateSku(sku);
        IsActive = true;
        StockQuantity = 0;
    }

    // Parameterless constructor for EF Core
    private Product() { }

    /// <summary>
    /// Gets the product name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the product description
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the product price
    /// </summary>
    public Money Price { get; private set; } = null!;

    /// <summary>
    /// Gets the product category
    /// </summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the product SKU (Stock Keeping Unit)
    /// </summary>
    public string SKU { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the stock quantity
    /// </summary>
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the product is active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the product tags
    /// </summary>
    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    /// <summary>
    /// Gets or sets the product image URL
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Updates the product information
    /// </summary>
    /// <param name="name">The new product name</param>
    /// <param name="description">The new product description</param>
    /// <param name="price">The new product price</param>
    /// <param name="category">The new product category</param>
    /// <param name="userId">The user making the update</param>
    public void UpdateProduct(string name, string description, Money price, string category, string userId)
    {
        Name = ValidateName(name);
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Price = price ?? throw new ArgumentNullException(nameof(price));
        Category = ValidateCategory(category);
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Updates the stock quantity
    /// </summary>
    /// <param name="quantity">The new stock quantity</param>
    /// <param name="userId">The user making the update</param>
    public void UpdateStock(int quantity, string userId)
    {
        if (quantity < 0)
            throw new ArgumentException("Stock quantity cannot be negative", nameof(quantity));

        StockQuantity = quantity;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Activates the product
    /// </summary>
    /// <param name="userId">The user making the update</param>
    public void Activate(string userId)
    {
        IsActive = true;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Deactivates the product
    /// </summary>
    /// <param name="userId">The user making the update</param>
    public void Deactivate(string userId)
    {
        IsActive = false;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Adds a tag to the product
    /// </summary>
    /// <param name="tag">The tag to add</param>
    /// <param name="userId">The user making the update</param>
    public void AddTag(string tag, string userId)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        if (!_tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            _tags.Add(tag);
            UpdateAuditInfo(userId);
        }
    }

    /// <summary>
    /// Removes a tag from the product
    /// </summary>
    /// <param name="tag">The tag to remove</param>
    /// <param name="userId">The user making the update</param>
    public void RemoveTag(string tag, string userId)
    {
        if (_tags.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)) > 0)
        {
            UpdateAuditInfo(userId);
        }
    }

    /// <summary>
    /// Sets the product image URL
    /// </summary>
    /// <param name="imageUrl">The image URL</param>
    /// <param name="userId">The user making the update</param>
    public void SetImageUrl(string? imageUrl, string userId)
    {
        ImageUrl = imageUrl;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Checks if the product is in stock
    /// </summary>
    /// <returns>True if the product is in stock, false otherwise</returns>
    public bool IsInStock() => StockQuantity > 0;

    /// <summary>
    /// Checks if the product is available for purchase
    /// </summary>
    /// <returns>True if the product is available, false otherwise</returns>
    public bool IsAvailable() => IsActive && IsInStock();

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Product name cannot exceed 200 characters", nameof(name));

        return name.Trim();
    }

    private static string ValidateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Product category cannot be empty", nameof(category));

        if (category.Length > 100)
            throw new ArgumentException("Product category cannot exceed 100 characters", nameof(category));

        return category.Trim();
    }

    private static string ValidateSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("Product SKU cannot be empty", nameof(sku));

        if (sku.Length > 50)
            throw new ArgumentException("Product SKU cannot exceed 50 characters", nameof(sku));

        return sku.Trim().ToUpperInvariant();
    }
}