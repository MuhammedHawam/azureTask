using AzureProductApi.Domain.Entities;
using AzureProductApi.Domain.Enums;
using AzureProductApi.Domain.Interfaces;
using AzureProductApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AzureProductApi.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IOutletRepository
/// </summary>
public class OutletRepository : IOutletRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OutletRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the OutletRepository class
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="logger">The logger</param>
    public OutletRepository(ApplicationDbContext context, ILogger<OutletRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<Outlet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlet by ID: {OutletId}", id);
        return await _context.Outlets.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets by name: {Name}", name);
        return await _context.Outlets
            .Where(o => o.Name.Contains(name))
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetAllAsync(
        string? tier = null,
        ChainType? chainType = null,
        bool? isActive = null,
        string? city = null,
        string? state = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets - Tier: {Tier}, ChainType: {ChainType}, IsActive: {IsActive}, City: {City}, State: {State}, Page: {PageNumber}, PageSize: {PageSize}",
            tier, chainType, isActive, city, state, pageNumber, pageSize);

        var query = _context.Outlets.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(tier))
        {
            query = query.Where(o => o.Tier.ToLower() == tier.ToLower());
        }

        if (chainType.HasValue)
        {
            query = query.Where(o => o.ChainType == chainType.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(o => EF.Property<string>(o.Address, "City").ToLower() == city.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(o => EF.Property<string>(o.Address, "State").ToLower() == state.ToLower());
        }

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        // Order by creation date (most recent first)
        query = query.OrderByDescending(o => o.CreatedAt);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(
        string? tier = null,
        ChainType? chainType = null,
        bool? isActive = null,
        string? city = null,
        string? state = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlet count - Tier: {Tier}, ChainType: {ChainType}, IsActive: {IsActive}, City: {City}, State: {State}",
            tier, chainType, isActive, city, state);

        var query = _context.Outlets.AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(tier))
        {
            query = query.Where(o => o.Tier.ToLower() == tier.ToLower());
        }

        if (chainType.HasValue)
        {
            query = query.Where(o => o.ChainType == chainType.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(o => EF.Property<string>(o.Address, "City").ToLower() == city.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(o => EF.Property<string>(o.Address, "State").ToLower() == state.ToLower());
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> SearchAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Searching outlets with term: {SearchTerm}, Page: {PageNumber}, PageSize: {PageSize}",
            searchTerm, pageNumber, pageSize);

        var query = _context.Outlets
            .Where(o => o.Name.Contains(searchTerm) || 
                       EF.Property<string>(o.Address, "Street").Contains(searchTerm) ||
                       EF.Property<string>(o.Address, "City").Contains(searchTerm) ||
                       EF.Property<string>(o.Address, "State").Contains(searchTerm))
            .OrderByDescending(o => o.CreatedAt);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetOutletsNeedingVisitAsync(
        int maxDaysSinceVisit = 30,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets needing visit - MaxDays: {MaxDaysSinceVisit}, Page: {PageNumber}, PageSize: {PageSize}",
            maxDaysSinceVisit, pageNumber, pageSize);

        var cutoffDate = DateTime.UtcNow.AddDays(-maxDaysSinceVisit);

        var query = _context.Outlets
            .Where(o => o.IsActive && (o.LastVisitDate == null || o.LastVisitDate < cutoffDate))
            .OrderBy(o => o.LastVisitDate ?? DateTime.MinValue);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetByRankRangeAsync(
        int minRank,
        int maxRank,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets by rank range: {MinRank}-{MaxRank}, Page: {PageNumber}, PageSize: {PageSize}",
            minRank, maxRank, pageNumber, pageSize);

        var query = _context.Outlets
            .Where(o => o.Rank >= minRank && o.Rank <= maxRank)
            .OrderBy(o => o.Rank);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetTiersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all outlet tiers");
        return await _context.Outlets
            .Select(o => o.Tier)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all outlet cities");
        return await _context.Outlets
            .Select(o => EF.Property<string>(o.Address, "City"))
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetHighPerformingOutletsAsync(
        decimal minAchievementPercentage = 100,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting high-performing outlets - MinAchievement: {MinAchievementPercentage}%, Page: {PageNumber}, PageSize: {PageSize}",
            minAchievementPercentage, pageNumber, pageSize);

        var query = _context.Outlets
            .Where(o => o.IsActive && o.VolumeTargetKg > 0 && 
                       (o.VolumeSoldKg / o.VolumeTargetKg * 100) >= minAchievementPercentage)
            .OrderByDescending(o => o.VolumeSoldKg / o.VolumeTargetKg);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Adding outlet: {Name}", outlet.Name);
        
        _context.Outlets.Add(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Successfully added outlet with ID: {OutletId}", outlet.Id);
        return outlet;
    }

    /// <inheritdoc />
    public async Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Updating outlet with ID: {OutletId}", outlet.Id);
        
        _context.Outlets.Update(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Successfully updated outlet with ID: {OutletId}", outlet.Id);
        return outlet;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting outlet with ID: {OutletId}", id);
        
        var outlet = await _context.Outlets.FindAsync(new object[] { id }, cancellationToken);
        if (outlet == null)
        {
            _logger.LogWarning("Outlet with ID {OutletId} not found for deletion", id);
            return false;
        }

        _context.Outlets.Remove(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Successfully deleted outlet with ID: {OutletId}", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsWithNameAndLocationAsync(
        string name, 
        string city, 
        string state, 
        Guid? excludeId = null, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Checking if outlet exists with name: {Name} in {City}, {State}, ExcludeId: {ExcludeId}", 
            name, city, state, excludeId);
        
        var query = _context.Outlets.Where(o => 
            o.Name.ToLower() == name.ToLower() &&
            EF.Property<string>(o.Address, "City").ToLower() == city.ToLower() &&
            EF.Property<string>(o.Address, "State").ToLower() == state.ToLower());
        
        if (excludeId.HasValue)
        {
            query = query.Where(o => o.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}