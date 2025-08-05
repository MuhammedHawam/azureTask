using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Enums;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace ImperialBackend.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IOutletRepository with optimized queries
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
        return await _context.Outlets
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetAllAsync(
        string? tier = null,
        ChainType? chainType = null,
        bool? isActive = null,
        string? city = null,
        string? state = null,
        string? searchTerm = null,
        int? minRank = null,
        int? maxRank = null,
        bool? needsVisit = null,
        int maxDaysSinceVisit = 30,
        bool? highPerforming = null,
        decimal minAchievementPercentage = 80.0m,
        int pageNumber = 1,
        int pageSize = 10,
        string sortBy = "CreatedAt",
        string sortDirection = "desc",
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets with filters - Page: {PageNumber}, PageSize: {PageSize}, SortBy: {SortBy}", 
            pageNumber, pageSize, sortBy);

        var query = _context.Outlets.AsNoTracking().AsQueryable();

        // Apply filters efficiently at database level
        query = ApplyFilters(query, tier, chainType, isActive, city, state, searchTerm, 
            minRank, maxRank, needsVisit, maxDaysSinceVisit, highPerforming, minAchievementPercentage);

        // Apply sorting at database level
        query = ApplySorting(query, sortBy, sortDirection);

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(
        string? tier = null,
        ChainType? chainType = null,
        bool? isActive = null,
        string? city = null,
        string? state = null,
        string? searchTerm = null,
        int? minRank = null,
        int? maxRank = null,
        bool? needsVisit = null,
        int maxDaysSinceVisit = 30,
        bool? highPerforming = null,
        decimal minAchievementPercentage = 80.0m,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlet count with filters");

        var query = _context.Outlets.AsQueryable();

        // Apply the same filters as GetAllAsync
        query = ApplyFilters(query, tier, chainType, isActive, city, state, searchTerm, 
            minRank, maxRank, needsVisit, maxDaysSinceVisit, highPerforming, minAchievementPercentage);

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Applies filters to the query efficiently at database level
    /// </summary>
    private IQueryable<Outlet> ApplyFilters(
        IQueryable<Outlet> query,
        string? tier,
        ChainType? chainType,
        bool? isActive,
        string? city,
        string? state,
        string? searchTerm,
        int? minRank,
        int? maxRank,
        bool? needsVisit,
        int maxDaysSinceVisit,
        bool? highPerforming,
        decimal minAchievementPercentage)
    {
        // Filter by tier
        if (!string.IsNullOrWhiteSpace(tier))
        {
            query = query.Where(o => o.Tier == tier);
        }

        // Filter by chain type
        if (chainType.HasValue)
        {
            query = query.Where(o => o.ChainType == chainType.Value);
        }

        // Filter by active status
        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        // Filter by city
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(o => EF.Property<string>(o.Address, "City") == city);
        }

        // Filter by state
        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(o => EF.Property<string>(o.Address, "State") == state);
        }

        // Search term filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o => o.Name.Contains(searchTerm) || 
                               EF.Property<string>(o.Address, "Street").Contains(searchTerm) ||
                               EF.Property<string>(o.Address, "City").Contains(searchTerm) ||
                               EF.Property<string>(o.Address, "State").Contains(searchTerm));
        }

        // Filter by rank range
        if (minRank.HasValue)
        {
            query = query.Where(o => o.Rank >= minRank.Value);
        }
        if (maxRank.HasValue)
        {
            query = query.Where(o => o.Rank <= maxRank.Value);
        }

        // Filter outlets needing visits
        if (needsVisit == true)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-maxDaysSinceVisit);
            query = query.Where(o => o.IsActive && (o.LastVisitDate == null || o.LastVisitDate < cutoffDate));
        }

        // Filter high-performing outlets
        if (highPerforming == true)
        {
            query = query.Where(o => o.IsActive && o.VolumeTargetKg > 0 && 
                               (o.VolumeSoldKg / o.VolumeTargetKg * 100) >= minAchievementPercentage);
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to the query efficiently at database level
    /// </summary>
    private IOrderedQueryable<Outlet> ApplySorting(IQueryable<Outlet> query, string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "name" => isDescending
                ? query.OrderByDescending(o => o.Name)
                : query.OrderBy(o => o.Name),
            "tier" => isDescending
                ? query.OrderByDescending(o => o.Tier)
                : query.OrderBy(o => o.Tier),
            "rank" => isDescending
                ? query.OrderByDescending(o => o.Rank)
                : query.OrderBy(o => o.Rank),
            "chaintype" => isDescending
                ? query.OrderByDescending(o => o.ChainType)
                : query.OrderBy(o => o.ChainType),
            "sales" => isDescending
                ? query.OrderByDescending(o => o.Sales.Amount)
                : query.OrderBy(o => o.Sales.Amount),
            "volumesold" => isDescending
                ? query.OrderByDescending(o => o.VolumeSoldKg)
                : query.OrderBy(o => o.VolumeSoldKg),
            "volumetarget" => isDescending
                ? query.OrderByDescending(o => o.VolumeTargetKg)
                : query.OrderBy(o => o.VolumeTargetKg),
            "targetachievement" => isDescending
                ? query.OrderByDescending(o => o.VolumeTargetKg > 0 ? (o.VolumeSoldKg / o.VolumeTargetKg * 100) : 0)
                : query.OrderBy(o => o.VolumeTargetKg > 0 ? (o.VolumeSoldKg / o.VolumeTargetKg * 100) : 0),
            "lastvisitdate" => isDescending
                ? query.OrderByDescending(o => o.LastVisitDate)
                : query.OrderBy(o => o.LastVisitDate),
            "city" => isDescending
                ? query.OrderByDescending(o => EF.Property<string>(o.Address, "City"))
                : query.OrderBy(o => EF.Property<string>(o.Address, "City")),
            "state" => isDescending
                ? query.OrderByDescending(o => EF.Property<string>(o.Address, "State"))
                : query.OrderBy(o => EF.Property<string>(o.Address, "State")),
            "updatedat" => isDescending
                ? query.OrderByDescending(o => o.UpdatedAt)
                : query.OrderBy(o => o.UpdatedAt),
            _ => isDescending
                ? query.OrderByDescending(o => o.CreatedAt)
                : query.OrderBy(o => o.CreatedAt)
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Enumerable.Empty<Outlet>();

        _logger.LogDebug("Getting outlets by name: {Name}", name);
        return await _context.Outlets
            .AsNoTracking()
            .Where(o => o.Name.Contains(name))
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetByTierAsync(string tier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tier))
            return Enumerable.Empty<Outlet>();

        _logger.LogDebug("Getting outlets by tier: {Tier}", tier);
        return await _context.Outlets
            .AsNoTracking()
            .Where(o => o.Tier == tier)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetByChainTypeAsync(ChainType chainType, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets by chain type: {ChainType}", chainType);
        return await _context.Outlets
            .AsNoTracking()
            .Where(o => o.ChainType == chainType)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetActiveOutletsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active outlets");
        return await _context.Outlets
            .AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> SearchAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<Outlet>();

        _logger.LogDebug("Searching outlets with term: {SearchTerm}, Page: {PageNumber}, PageSize: {PageSize}",
            searchTerm, pageNumber, pageSize);

        var skip = (pageNumber - 1) * pageSize;

        return await _context.Outlets
            .AsNoTracking()
            .Where(o => o.Name.Contains(searchTerm) || 
                       EF.Property<string>(o.Address, "Street").Contains(searchTerm) ||
                       EF.Property<string>(o.Address, "City").Contains(searchTerm) ||
                       EF.Property<string>(o.Address, "State").Contains(searchTerm))
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
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

        var skip = (pageNumber - 1) * pageSize;

        return await _context.Outlets
            .AsNoTracking()
            .Where(o => o.Rank >= minRank && o.Rank <= maxRank)
            .OrderBy(o => o.Rank)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetOutletsNeedingVisitAsync(
        int maxDaysSinceVisit = 30,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets needing visit - MaxDays: {MaxDays}, Page: {PageNumber}, PageSize: {PageSize}",
            maxDaysSinceVisit, pageNumber, pageSize);

        var cutoffDate = DateTime.UtcNow.AddDays(-maxDaysSinceVisit);
        var skip = (pageNumber - 1) * pageSize;

        return await _context.Outlets
            .AsNoTracking()
            .Where(o => o.IsActive && (o.LastVisitDate == null || o.LastVisitDate < cutoffDate))
            .OrderBy(o => o.LastVisitDate ?? DateTime.MinValue)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetHighPerformingOutletsAsync(
        decimal minAchievementPercentage = 80.0m,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting high performing outlets - MinAchievement: {MinAchievement}%, Page: {PageNumber}, PageSize: {PageSize}",
            minAchievementPercentage, pageNumber, pageSize);

        var skip = (pageNumber - 1) * pageSize;

        return await _context.Outlets
            .AsNoTracking()
            .Where(o => o.IsActive && o.VolumeTargetKg > 0 && 
                       (o.VolumeSoldKg / o.VolumeTargetKg * 100) >= minAchievementPercentage)
            .OrderByDescending(o => o.VolumeSoldKg / o.VolumeTargetKg)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        if (outlet == null)
            throw new ArgumentNullException(nameof(outlet));

        _logger.LogDebug("Adding new outlet: {OutletName}", outlet.Name);

        _context.Outlets.Add(outlet);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully added outlet with ID: {OutletId}", outlet.Id);
        return outlet;
    }

    /// <inheritdoc />
    public async Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        if (outlet == null)
            throw new ArgumentNullException(nameof(outlet));

        _logger.LogDebug("Updating outlet: {OutletId}", outlet.Id);

        _context.Outlets.Update(outlet);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully updated outlet: {OutletId}", outlet.Id);
        return outlet;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting outlet: {OutletId}", id);

        var outlet = await _context.Outlets.FindAsync(new object[] { id }, cancellationToken);
        if (outlet == null)
        {
            _logger.LogWarning("Outlet not found for deletion: {OutletId}", id);
            return false;
        }

        _context.Outlets.Remove(outlet);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully deleted outlet: {OutletId}", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Outlets.AnyAsync(o => o.Id == id, cancellationToken);
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
            o.Name == name &&
            EF.Property<string>(o.Address, "City") == city &&
            EF.Property<string>(o.Address, "State") == state);
        
        if (excludeId.HasValue)
        {
            query = query.Where(o => o.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetDistinctTiersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting distinct tiers");
        return await _context.Outlets
            .AsNoTracking()
            .Where(o => !string.IsNullOrEmpty(o.Tier))
            .Select(o => o.Tier)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetDistinctCitiesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting distinct cities");
        return await _context.Outlets
            .AsNoTracking()
            .Select(o => EF.Property<string>(o.Address, "City"))
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken);
    }
}