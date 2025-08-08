using ImperialBackend.Domain.Entities;
using ImperialBackend.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ImperialBackend.Infrastructure.Data;

/// <summary>
/// Application database context for Databricks using CData EF Core provider
/// </summary>
public class ApplicationDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the ApplicationDbContext class
    /// </summary>
    /// <param name="options">The database context options</param>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Outlets DbSet
    /// </summary>
    public DbSet<Outlet> Outlets { get; set; } = null!;

    /// <summary>
    /// Configures the model that was discovered by convention from the entity types
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure specific entities
        modelBuilder.ApplyConfiguration(new OutletConfiguration());
    }

    /// <summary>
    /// Saves all changes made in this context to the database
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete</param>
    /// <returns>The number of state entries written to the database</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Set audit information for entities
        foreach (var entry in ChangeTracker.Entries<Domain.Common.BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // CreatedAt and CreatedBy are set in the entity constructor and methods
                    break;
                case EntityState.Modified:
                    // UpdatedAt and UpdatedBy are set in the entity methods
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}