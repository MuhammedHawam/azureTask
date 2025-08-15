using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Infrastructure.Repositories;

public class OutletRepository : IOutletRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OutletRepository> _logger;

    public OutletRepository(ApplicationDbContext context, ILogger<OutletRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Outlet?> GetByIdAsync(string outletIdentifier, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlet by identifier: {OutletIdentifier}", outletIdentifier);
        return await _context.Outlets
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OutletIdentifier == outletIdentifier, cancellationToken);
    }

    public async Task<IEnumerable<Outlet>> GetAllAsync(
        int? year = null,
        int? week = null,
        string? healthStatus = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 10,
        string sortBy = "StoreRank",
        string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets: Page {PageNumber}, Size {PageSize}, Sort {SortBy} {SortDirection}", pageNumber, pageSize, sortBy, sortDirection);

        var query = _context.Outlets.AsNoTracking().AsQueryable();

        query = ApplyFilters(query, year, week, healthStatus, searchTerm);
        query = ApplySorting(query, sortBy, sortDirection);

        var skip = (pageNumber - 1) * pageSize;
        query = query.Skip(skip).Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(
        int? year = null,
        int? week = null,
        string? healthStatus = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlet count with filters");

        var query = _context.Outlets.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, year, week, healthStatus, searchTerm);

        return await query.CountAsync(cancellationToken);
    }

    public async Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        if (outlet == null)
            throw new ArgumentNullException(nameof(outlet));

        _logger.LogDebug("Adding new outlet: {OutletName}", outlet.OutletName);
        _context.Outlets.Add(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully added outlet with Identifier: {OutletIdentifier}", outlet.OutletIdentifier);
        return outlet;
    }

    public async Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        if (outlet == null)
            throw new ArgumentNullException(nameof(outlet));

        _logger.LogDebug("Updating outlet: {OutletIdentifier}", outlet.OutletIdentifier);
        _context.Outlets.Update(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully updated outlet: {OutletIdentifier}", outlet.OutletIdentifier);
        return outlet;
    }

    public async Task<bool> DeleteAsync(string outletIdentifier, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting outlet: {OutletIdentifier}", outletIdentifier);

        var outlet = await _context.Outlets.FirstOrDefaultAsync(o => o.OutletIdentifier == outletIdentifier, cancellationToken);
        if (outlet == null)
        {
            _logger.LogWarning("Outlet not found for deletion: {OutletIdentifier}", outletIdentifier);
            return false;
        }

        _context.Outlets.Remove(outlet);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully deleted outlet: {OutletIdentifier}", outletIdentifier);
        return true;
    }

    public async Task<bool> ExistsAsync(string outletIdentifier, CancellationToken cancellationToken = default)
    {
        return await _context.Outlets.AnyAsync(o => o.OutletIdentifier == outletIdentifier, cancellationToken);
    }

    private static IQueryable<Outlet> ApplyFilters(
        IQueryable<Outlet> query,
        int? year,
        int? week,
        string? healthStatus,
        string? searchTerm)
    {
        if (year.HasValue)
        {
            query = query.Where(o => o.Year == year.Value);
        }

        if (week.HasValue)
        {
            query = query.Where(o => o.Week == week.Value);
        }

        if (!string.IsNullOrWhiteSpace(healthStatus))
        {
            query = query.Where(o => o.HealthStatus == healthStatus);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o =>
                o.OutletName.Contains(searchTerm) ||
                o.OutletIdentifier.Contains(searchTerm) ||
                o.AddressLine1.Contains(searchTerm) ||
                o.State.Contains(searchTerm) ||
                o.County.Contains(searchTerm));
        }

        return query;
    }

    private static IOrderedQueryable<Outlet> ApplySorting(IQueryable<Outlet> query, string sortBy, string sortDirection)
    {
        var desc = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return sortBy.ToLowerInvariant() switch
        {
            "year" => desc ? query.OrderByDescending(o => o.Year) : query.OrderBy(o => o.Year),
            "week" => desc ? query.OrderByDescending(o => o.Week) : query.OrderBy(o => o.Week),
            "totalouterquantity" => desc ? query.OrderByDescending(o => o.TotalOuterQuantity) : query.OrderBy(o => o.TotalOuterQuantity),
            "countouterquantity" => desc ? query.OrderByDescending(o => o.CountOuterQuantity) : query.OrderBy(o => o.CountOuterQuantity),
            "totalsales6w" => desc ? query.OrderByDescending(o => o.TotalSales6w) : query.OrderBy(o => o.TotalSales6w),
            "mean" => desc ? query.OrderByDescending(o => o.Mean) : query.OrderBy(o => o.Mean),
            "lowerlimit" => desc ? query.OrderByDescending(o => o.LowerLimit) : query.OrderBy(o => o.LowerLimit),
            "upperlimit" => desc ? query.OrderByDescending(o => o.UpperLimit) : query.OrderBy(o => o.UpperLimit),
            "healthstatus" => desc ? query.OrderByDescending(o => o.HealthStatus) : query.OrderBy(o => o.HealthStatus),
            "outletname" => desc ? query.OrderByDescending(o => o.OutletName) : query.OrderBy(o => o.OutletName),
            "state" => desc ? query.OrderByDescending(o => o.State) : query.OrderBy(o => o.State),
            "county" => desc ? query.OrderByDescending(o => o.County) : query.OrderBy(o => o.County),
            _ => desc ? query.OrderByDescending(o => o.StoreRank) : query.OrderBy(o => o.StoreRank)
        };
    }
}