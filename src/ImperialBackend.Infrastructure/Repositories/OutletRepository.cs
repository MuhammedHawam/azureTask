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
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets - Page: {PageNumber}, PageSize: {PageSize}", pageNumber, pageSize);

        var skip = (pageNumber - 1) * pageSize;

        return await _context.Outlets
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting total outlet count");
        return await _context.Outlets.CountAsync(cancellationToken);
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
            return await GetAllAsync(pageNumber, pageSize, cancellationToken);

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