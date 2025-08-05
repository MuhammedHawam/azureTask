namespace AzureProductApi.Application.DTOs;

/// <summary>
/// Data transfer object for product information
/// </summary>
public class ProductDto
{
    /// <summary>
    /// Gets or sets the product identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price amount
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product price currency
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product SKU
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stock quantity
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the product is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the product tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the product image URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the product
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the product
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets a value indicating whether the product is in stock
    /// </summary>
    public bool IsInStock => StockQuantity > 0;

    /// <summary>
    /// Gets a value indicating whether the product is available
    /// </summary>
    public bool IsAvailable => IsActive && IsInStock;
}

/// <summary>
/// Data transfer object for creating a new product
/// </summary>
public class CreateProductDto
{
    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price amount
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product price currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the product category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product SKU
    /// </summary>
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the initial stock quantity
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Gets or sets the product tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the product image URL
    /// </summary>
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Data transfer object for updating a product
/// </summary>
public class UpdateProductDto
{
    /// <summary>
    /// Gets or sets the product name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price amount
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the product price currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the product category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stock quantity
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Gets or sets the product tags
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the product image URL
    /// </summary>
    public string? ImageUrl { get; set; }
}