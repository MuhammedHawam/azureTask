using System.Data;
using System.Data.SqlClient;
using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Enums;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Infrastructure.Data;
using ImperialBackend.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImperialBackend.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IOutletRepository optimized for Databricks
/// </summary>
public class OutletRepository : IOutletRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OutletRepository> _logger;
    private readonly DatabricksSettings _dbxSettings;
    private readonly string? _connectionString;

    public OutletRepository(ApplicationDbContext context, ILogger<OutletRepository> logger, IOptions<DatabricksSettings> databricksOptions, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbxSettings = databricksOptions?.Value ?? new DatabricksSettings();
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<Outlet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_dbxSettings.UseAdoForOutlets && _connectionString is not null && !string.IsNullOrWhiteSpace(_dbxSettings.OutletsTable))
        {
            var table = _dbxSettings.OutletsTable!;
            var schema = _dbxSettings.Schema;
            var full = string.IsNullOrWhiteSpace(schema) ? Quote(table) : $"{Quote(schema)}.{Quote(table)}";
            var sql = $"SELECT TOP 1 * FROM {full} WHERE [Id] = @Id";
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = id });
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return MapOutlet(reader);
            }
            return null;
        }

        _logger.LogDebug("Getting outlet by ID: {OutletId}", id);
        return await _context.Outlets
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

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
        if (_dbxSettings.UseAdoForOutlets && _connectionString is not null && !string.IsNullOrWhiteSpace(_dbxSettings.OutletsTable))
        {
            var table = _dbxSettings.OutletsTable!;
            var schema = _dbxSettings.Schema;
            var full = string.IsNullOrWhiteSpace(schema) ? Quote(table) : $"{Quote(schema)}.{Quote(table)}";

            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(tier)) { whereClauses.Add("[Tier] = @Tier"); parameters.Add(new SqlParameter("@Tier", SqlDbType.NVarChar, 50) { Value = tier }); }
            if (chainType.HasValue) { whereClauses.Add("[ChainType] = @ChainType"); parameters.Add(new SqlParameter("@ChainType", SqlDbType.Int) { Value = (int)chainType.Value }); }
            if (isActive.HasValue) { whereClauses.Add("[IsActive] = @IsActive"); parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive.Value }); }
            if (!string.IsNullOrWhiteSpace(city)) { whereClauses.Add("[City] = @City"); parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = city }); }
            if (!string.IsNullOrWhiteSpace(state)) { whereClauses.Add("[State] = @State"); parameters.Add(new SqlParameter("@State", SqlDbType.NVarChar, 100) { Value = state }); }
            if (!string.IsNullOrWhiteSpace(searchTerm)) { whereClauses.Add("([Name] LIKE @Search OR [Street] LIKE @Search OR [City] LIKE @Search OR [State] LIKE @Search)"); parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 200) { Value = "%" + searchTerm + "%" }); }
            if (minRank.HasValue) { whereClauses.Add("[Rank] >= @MinRank"); parameters.Add(new SqlParameter("@MinRank", SqlDbType.Int) { Value = minRank.Value }); }
            if (maxRank.HasValue) { whereClauses.Add("[Rank] <= @MaxRank"); parameters.Add(new SqlParameter("@MaxRank", SqlDbType.Int) { Value = maxRank.Value }); }
            if (needsVisit == true) { whereClauses.Add("([IsActive] = 1 AND ([LastVisitDate] IS NULL OR [LastVisitDate] < @Cutoff))"); parameters.Add(new SqlParameter("@Cutoff", SqlDbType.DateTime2) { Value = DateTime.UtcNow.AddDays(-maxDaysSinceVisit) }); }
            if (highPerforming == true) { whereClauses.Add("([IsActive] = 1 AND [VolumeTargetKg] > 0 AND ([VolumeSoldKg] / [VolumeTargetKg] * 100) >= @MinAch)"); parameters.Add(new SqlParameter("@MinAch", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = minAchievementPercentage }); }

            var skip = (pageNumber - 1) * pageSize;
            var sortCol = MapSortableColumn(sortBy);
            var isDesc = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

            var sql = $@"SELECT * FROM {full}
{(whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty)}
ORDER BY {sortCol} {isDesc}
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parameters) cmd.Parameters.Add(p);
            cmd.Parameters.Add(new SqlParameter("@Skip", SqlDbType.Int) { Value = skip });
            cmd.Parameters.Add(new SqlParameter("@Take", SqlDbType.Int) { Value = pageSize });

            var results = new List<Outlet>();
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                results.Add(MapOutlet(reader));
            }
            return results;
        }

        _logger.LogDebug("Getting outlets with filters - Page: {PageNumber}, PageSize: {PageSize}, SortBy: {SortBy}", 
            pageNumber, pageSize, sortBy);

        var query = _context.Outlets.AsNoTracking().AsQueryable();

        // Apply filters efficiently at database level
        query = ApplyFilters(query, tier, chainType, isActive, city, state, searchTerm, 
            minRank, maxRank, needsVisit, maxDaysSinceVisit, highPerforming, minAchievementPercentage);

        // Apply sorting at database level
        query = ApplySorting(query, sortBy, sortDirection);

        // Apply pagination
        var skipEf = (pageNumber - 1) * pageSize;
        query = query.Skip(skipEf).Take(pageSize);

        return await query.ToListAsync(cancellationToken);
    }

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
        if (_dbxSettings.UseAdoForOutlets && _connectionString is not null && !string.IsNullOrWhiteSpace(_dbxSettings.OutletsTable))
        {
            var table = _dbxSettings.OutletsTable!;
            var schema = _dbxSettings.Schema;
            var full = string.IsNullOrWhiteSpace(schema) ? Quote(table) : $"{Quote(schema)}.{Quote(table)}";

            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(tier)) { whereClauses.Add("[Tier] = @Tier"); parameters.Add(new SqlParameter("@Tier", SqlDbType.NVarChar, 50) { Value = tier }); }
            if (chainType.HasValue) { whereClauses.Add("[ChainType] = @ChainType"); parameters.Add(new SqlParameter("@ChainType", SqlDbType.Int) { Value = (int)chainType.Value }); }
            if (isActive.HasValue) { whereClauses.Add("[IsActive] = @IsActive"); parameters.Add(new SqlParameter("@IsActive", SqlDbType.Bit) { Value = isActive.Value }); }
            if (!string.IsNullOrWhiteSpace(city)) { whereClauses.Add("[City] = @City"); parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = city }); }
            if (!string.IsNullOrWhiteSpace(state)) { whereClauses.Add("[State] = @State"); parameters.Add(new SqlParameter("@State", SqlDbType.NVarChar, 100) { Value = state }); }
            if (!string.IsNullOrWhiteSpace(searchTerm)) { whereClauses.Add("([Name] LIKE @Search OR [Street] LIKE @Search OR [City] LIKE @Search OR [State] LIKE @Search)"); parameters.Add(new SqlParameter("@Search", SqlDbType.NVarChar, 200) { Value = "%" + searchTerm + "%" }); }
            if (minRank.HasValue) { whereClauses.Add("[Rank] >= @MinRank"); parameters.Add(new SqlParameter("@MinRank", SqlDbType.Int) { Value = minRank.Value }); }
            if (maxRank.HasValue) { whereClauses.Add("[Rank] <= @MaxRank"); parameters.Add(new SqlParameter("@MaxRank", SqlDbType.Int) { Value = maxRank.Value }); }
            if (needsVisit == true) { whereClauses.Add("([IsActive] = 1 AND ([LastVisitDate] IS NULL OR [LastVisitDate] < @Cutoff))"); parameters.Add(new SqlParameter("@Cutoff", SqlDbType.DateTime2) { Value = DateTime.UtcNow.AddDays(-maxDaysSinceVisit) }); }
            if (highPerforming == true) { whereClauses.Add("([IsActive] = 1 AND [VolumeTargetKg] > 0 AND ([VolumeSoldKg] / [VolumeTargetKg] * 100) >= @MinAch)"); parameters.Add(new SqlParameter("@MinAch", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = minAchievementPercentage }); }

            var sql = $@"SELECT COUNT(1) FROM {full}
{(whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : string.Empty)}";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new SqlCommand(sql, conn);
            foreach (var p in parameters) cmd.Parameters.Add(p);
            var count = (int) (await cmd.ExecuteScalarAsync(cancellationToken) ?? 0);
            return count;
        }

        _logger.LogDebug("Getting outlet count with filters");

        var query = _context.Outlets.AsQueryable();

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

        if (_dbxSettings.UseAdoForOutlets && _connectionString is not null && !string.IsNullOrWhiteSpace(_dbxSettings.OutletsTable))
        {
            var table = _dbxSettings.OutletsTable!;
            var schema = _dbxSettings.Schema;
            var full = string.IsNullOrWhiteSpace(schema) ? Quote(table) : $"{Quote(schema)}.{Quote(table)}";
            var sql = $@"INSERT INTO {full} (
    [Id],[Name],[Tier],[Rank],[ChainType],[LastVisitDate],[SalesAmount],[SalesCurrency],[VolumeSoldKg],[VolumeTargetKg],[Street],[City],[State],[PostalCode],[Country],[IsActive],[CreatedAt],[UpdatedAt],[CreatedBy],[UpdatedBy]
) VALUES (
    @Id,@Name,@Tier,@Rank,@ChainType,@LastVisitDate,@SalesAmount,@SalesCurrency,@VolumeSoldKg,@VolumeTargetKg,@Street,@City,@State,@PostalCode,@Country,@IsActive,@CreatedAt,@UpdatedAt,@CreatedBy,@UpdatedBy
)";

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddRange(BuildOutletParameters(outlet));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            return outlet;
        }

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

    private static string Quote(string id) => $"[{id}]";

    private static SqlParameter[] BuildOutletParameters(Outlet o)
    {
        return new[]
        {
            new SqlParameter("@Id", SqlDbType.UniqueIdentifier) { Value = o.Id },
            new SqlParameter("@Name", SqlDbType.NVarChar, 200) { Value = o.Name },
            new SqlParameter("@Tier", SqlDbType.NVarChar, 50) { Value = o.Tier },
            new SqlParameter("@Rank", SqlDbType.Int) { Value = o.Rank },
            new SqlParameter("@ChainType", SqlDbType.Int) { Value = (int)o.ChainType },
            new SqlParameter("@LastVisitDate", SqlDbType.DateTime2) { Value = (object?)o.LastVisitDate ?? DBNull.Value },
            new SqlParameter("@SalesAmount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = o.Sales.Amount },
            new SqlParameter("@SalesCurrency", SqlDbType.NVarChar, 3) { Value = o.Sales.Currency },
            new SqlParameter("@VolumeSoldKg", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = o.VolumeSoldKg },
            new SqlParameter("@VolumeTargetKg", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = o.VolumeTargetKg },
            new SqlParameter("@Street", SqlDbType.NVarChar, 200) { Value = o.Address.Street },
            new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = o.Address.City },
            new SqlParameter("@State", SqlDbType.NVarChar, 100) { Value = o.Address.State },
            new SqlParameter("@PostalCode", SqlDbType.NVarChar, 20) { Value = o.Address.PostalCode },
            new SqlParameter("@Country", SqlDbType.NVarChar, 100) { Value = o.Address.Country },
            new SqlParameter("@IsActive", SqlDbType.Bit) { Value = o.IsActive },
            new SqlParameter("@CreatedAt", SqlDbType.DateTime2) { Value = o.CreatedAt },
            new SqlParameter("@UpdatedAt", SqlDbType.DateTime2) { Value = (object?)o.UpdatedAt ?? DBNull.Value },
            new SqlParameter("@CreatedBy", SqlDbType.NVarChar, 100) { Value = (object?)o.CreatedBy ?? DBNull.Value },
            new SqlParameter("@UpdatedBy", SqlDbType.NVarChar, 100) { Value = (object?)o.UpdatedBy ?? DBNull.Value }
        };
    }

    private static Outlet MapOutlet(SqlDataReader reader)
    {
        var outlet = (Outlet)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(Outlet));
        var id = reader.GetGuid(reader.GetOrdinal("Id"));
        var name = reader.GetString(reader.GetOrdinal("Name"));
        var tier = reader.GetString(reader.GetOrdinal("Tier"));
        var rank = reader.GetInt32(reader.GetOrdinal("Rank"));
        var chainType = (ChainType)reader.GetInt32(reader.GetOrdinal("ChainType"));
        var city = GetNullableString(reader, "City");
        var state = GetNullableString(reader, "State");
        var street = GetNullableString(reader, "Street");
        var postalCode = GetNullableString(reader, "PostalCode");
        var country = GetNullableString(reader, "Country");

        // Use reflection to set private setters
        typeof(ImperialBackend.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(outlet, id);
        typeof(Outlet).GetProperty("Name")!.SetValue(outlet, name);
        typeof(Outlet).GetProperty("Tier")!.SetValue(outlet, tier);
        typeof(Outlet).GetProperty("Rank")!.SetValue(outlet, rank);
        typeof(Outlet).GetProperty("ChainType")!.SetValue(outlet, chainType);

        var addressType = typeof(Outlet).GetProperty("Address")!.PropertyType;
        var address = Activator.CreateInstance(addressType, nonPublic: true)!;
        addressType.GetProperty("Street")!.SetValue(address, street ?? string.Empty);
        addressType.GetProperty("City")!.SetValue(address, city ?? string.Empty);
        addressType.GetProperty("State")!.SetValue(address, state ?? string.Empty);
        addressType.GetProperty("PostalCode")!.SetValue(address, postalCode ?? string.Empty);
        addressType.GetProperty("Country")!.SetValue(address, country ?? string.Empty);
        typeof(Outlet).GetProperty("Address")!.SetValue(outlet, address);

        var lastVisit = GetNullableDateTime(reader, "LastVisitDate");
        typeof(Outlet).GetProperty("LastVisitDate")!.SetValue(outlet, lastVisit);

        var salesAmount = GetNullableDecimal(reader, "SalesAmount") ?? 0m;
        var salesCurrency = GetNullableString(reader, "SalesCurrency") ?? "USD";
        var moneyType = typeof(Outlet).GetProperty("Sales")!.PropertyType;
        var sales = Activator.CreateInstance(moneyType, nonPublic: true)!;
        moneyType.GetProperty("Amount")!.SetValue(sales, salesAmount);
        moneyType.GetProperty("Currency")!.SetValue(sales, salesCurrency);
        typeof(Outlet).GetProperty("Sales")!.SetValue(outlet, sales);

        typeof(Outlet).GetProperty("VolumeSoldKg")!.SetValue(outlet, GetNullableDecimal(reader, "VolumeSoldKg") ?? 0m);
        typeof(Outlet).GetProperty("VolumeTargetKg")!.SetValue(outlet, GetNullableDecimal(reader, "VolumeTargetKg") ?? 0m);
        typeof(Outlet).GetProperty("IsActive")!.SetValue(outlet, reader.GetBoolean(reader.GetOrdinal("IsActive")));

        typeof(ImperialBackend.Domain.Common.BaseEntity).GetProperty("CreatedAt")!.SetValue(outlet, reader.GetDateTime(reader.GetOrdinal("CreatedAt")));
        typeof(ImperialBackend.Domain.Common.BaseEntity).GetProperty("UpdatedAt")!.SetValue(outlet, GetNullableDateTime(reader, "UpdatedAt"));
        typeof(ImperialBackend.Domain.Common.BaseEntity).GetProperty("CreatedBy")!.SetValue(outlet, GetNullableString(reader, "CreatedBy"));
        typeof(ImperialBackend.Domain.Common.BaseEntity).GetProperty("UpdatedBy")!.SetValue(outlet, GetNullableString(reader, "UpdatedBy"));

        return outlet;
    }

    private static string? GetNullableString(SqlDataReader r, string name)
    {
        var idx = r.GetOrdinal(name);
        return r.IsDBNull(idx) ? null : r.GetString(idx);
    }
    private static DateTime? GetNullableDateTime(SqlDataReader r, string name)
    {
        var idx = r.GetOrdinal(name);
        return r.IsDBNull(idx) ? (DateTime?)null : r.GetDateTime(idx);
    }
    private static decimal? GetNullableDecimal(SqlDataReader r, string name)
    {
        var idx = r.GetOrdinal(name);
        return r.IsDBNull(idx) ? (decimal?)null : r.GetDecimal(idx);
    }

    private static string MapSortableColumn(string sortBy)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "name" => "Name",
            "tier" => "Tier",
            "rank" => "Rank",
            "chaintype" => "ChainType",
            "sales" => "SalesAmount",
            "volumesold" => "VolumeSoldKg",
            "volumetarget" => "VolumeTargetKg",
            "targetachievement" => "VolumeTargetKg > 0 ? (VolumeSoldKg / VolumeTargetKg * 100) : 0",
            "lastvisitdate" => "LastVisitDate",
            "city" => "City",
            "state" => "State",
            "updatedat" => "UpdatedAt",
            _ => "CreatedAt"
        };
    }
}