using AzureProductApi.Domain.Entities;
using AzureProductApi.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzureProductApi.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Product entity
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>
    /// Configures the Product entity
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the domain

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.SKU)
            .IsUnique()
            .HasDatabaseName("IX_Products_SKU");

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        // Configure Money value object
        builder.OwnsOne(p => p.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure Tags as JSON (EF Core 7+ feature)
        builder.Property(p => p.Tags)
            .HasConversion(
                tags => string.Join(';', tags),
                value => value.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
            .HasColumnName("Tags")
            .HasMaxLength(1000);

        // Audit properties
        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100);

        // Indexes for performance
        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_Products_Category");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Products_IsActive");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_Products_CreatedAt");

        builder.HasIndex(p => new { p.Category, p.IsActive })
            .HasDatabaseName("IX_Products_Category_IsActive");
    }
}