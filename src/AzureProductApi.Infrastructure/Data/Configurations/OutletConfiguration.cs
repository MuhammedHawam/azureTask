using AzureProductApi.Domain.Entities;
using AzureProductApi.Domain.Enums;
using AzureProductApi.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AzureProductApi.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Outlet entity
/// </summary>
public class OutletConfiguration : IEntityTypeConfiguration<Outlet>
{
    /// <summary>
    /// Configures the Outlet entity
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.ToTable("Outlets");

        // Primary key
        builder.HasKey(o => o.Id);

        // Properties
        builder.Property(o => o.Id)
            .ValueGeneratedNever(); // We generate GUIDs in the domain

        builder.Property(o => o.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.Tier)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Rank)
            .IsRequired();

        builder.Property(o => o.ChainType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(o => o.LastVisitDate);

        builder.Property(o => o.VolumeSoldKg)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.VolumeTargetKg)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.IsActive)
            .IsRequired();

        // Configure Money value object for Sales
        builder.OwnsOne(o => o.Sales, salesBuilder =>
        {
            salesBuilder.Property(m => m.Amount)
                .HasColumnName("SalesAmount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            salesBuilder.Property(m => m.Currency)
                .HasColumnName("SalesCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure Address value object
        builder.OwnsOne(o => o.Address, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street)
                .HasColumnName("Street")
                .HasMaxLength(200)
                .IsRequired();

            addressBuilder.Property(a => a.City)
                .HasColumnName("City")
                .HasMaxLength(100)
                .IsRequired();

            addressBuilder.Property(a => a.State)
                .HasColumnName("State")
                .HasMaxLength(100)
                .IsRequired();

            addressBuilder.Property(a => a.PostalCode)
                .HasColumnName("PostalCode")
                .HasMaxLength(20)
                .IsRequired();

            addressBuilder.Property(a => a.Country)
                .HasColumnName("Country")
                .HasMaxLength(100)
                .IsRequired();
        });

        // Audit properties
        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt);

        builder.Property(o => o.CreatedBy)
            .HasMaxLength(100);

        builder.Property(o => o.UpdatedBy)
            .HasMaxLength(100);

        // Indexes for performance
        builder.HasIndex(o => o.Name)
            .HasDatabaseName("IX_Outlets_Name");

        builder.HasIndex(o => o.Tier)
            .HasDatabaseName("IX_Outlets_Tier");

        builder.HasIndex(o => o.Rank)
            .HasDatabaseName("IX_Outlets_Rank");

        builder.HasIndex(o => o.ChainType)
            .HasDatabaseName("IX_Outlets_ChainType");

        builder.HasIndex(o => o.IsActive)
            .HasDatabaseName("IX_Outlets_IsActive");

        builder.HasIndex(o => o.CreatedAt)
            .HasDatabaseName("IX_Outlets_CreatedAt");

        builder.HasIndex(o => o.LastVisitDate)
            .HasDatabaseName("IX_Outlets_LastVisitDate");

        // Composite indexes for common query patterns
        builder.HasIndex(o => new { o.Tier, o.IsActive })
            .HasDatabaseName("IX_Outlets_Tier_IsActive");

        builder.HasIndex(o => new { o.ChainType, o.IsActive })
            .HasDatabaseName("IX_Outlets_ChainType_IsActive");

        builder.HasIndex("City", "State")
            .HasDatabaseName("IX_Outlets_City_State");

        builder.HasIndex("City", "State", "Name")
            .HasDatabaseName("IX_Outlets_City_State_Name")
            .IsUnique();
    }
}