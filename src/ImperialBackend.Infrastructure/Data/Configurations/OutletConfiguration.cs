using ImperialBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImperialBackend.Infrastructure.Data.Configurations;

public class OutletConfiguration : IEntityTypeConfiguration<Outlet>
{
    public void Configure(EntityTypeBuilder<Outlet> builder)
    {
        builder.ToTable("factoutlet_view", "mart_it");

        builder.Ignore(o => o.Id);
        builder.Ignore(o => o.CreatedAt);
        builder.Ignore(o => o.UpdatedAt);
        builder.Ignore(o => o.CreatedBy);
        builder.Ignore(o => o.UpdatedBy);

        builder.HasKey(o => o.OutletIdentifier);

        builder.Property(o => o.OutletIdentifier).HasColumnName("OutletIdentifier").HasMaxLength(100);

        builder.Property(o => o.Year).HasColumnName("year");
        builder.Property(o => o.Week).HasColumnName("week");
        builder.Property(o => o.TotalOuterQuantity).HasColumnName("TotalOuterQuantity");
        builder.Property(o => o.CountOuterQuantity).HasColumnName("CountOuterQuantity");
        builder.Property(o => o.TotalSales6w).HasColumnName("total_sales_6w");
        builder.Property(o => o.Mean).HasColumnName("mean");
        builder.Property(o => o.LowerLimit).HasColumnName("lowerlimit");
        builder.Property(o => o.UpperLimit).HasColumnName("upperlimit");
        builder.Property(o => o.HealthStatus).HasColumnName("health_status").HasMaxLength(50);
        builder.Property(o => o.StoreRank).HasColumnName("store_rank");
        builder.Property(o => o.OutletName).HasColumnName("OutletName").HasMaxLength(200);
        builder.Property(o => o.AddressLine1).HasColumnName("AddressLine1").HasMaxLength(200);
        builder.Property(o => o.State).HasColumnName("State").HasMaxLength(50);
        builder.Property(o => o.County).HasColumnName("County").HasMaxLength(100);
    }
}