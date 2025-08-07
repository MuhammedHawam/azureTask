using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Enums;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Domain.ValueObjects;
using ImperialBackend.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Dapper;
using System.Text;

namespace ImperialBackend.Infrastructure.Repositories;

/// <summary>
/// Databricks implementation of IOutletRepository using Dapper
/// </summary>
public class OutletRepository : IOutletRepository
{
    private readonly IDatabricksConnectionService _connectionService;
    private readonly ILogger<OutletRepository> _logger;
    private readonly string _tableName;

    /// <summary>
    /// Initializes a new instance of the OutletRepository class
    /// </summary>
    /// <param name="connectionService">The Databricks connection service</param>
    /// <param name="logger">The logger</param>
    public OutletRepository(IDatabricksConnectionService connectionService, ILogger<OutletRepository> logger)
    {
        _connectionService = connectionService ?? throw new ArgumentNullException(nameof(connectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tableName = _connectionService.GetFullTableName("outlets");
    }

    /// <inheritdoc />
    public async Task<Outlet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlet by ID: {OutletId}", id);
        
        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE Id = ?";

        var result = await connection.QueryFirstOrDefaultAsync<OutletDataModel>(sql, new { Id = id.ToString() });
        return result?.ToOutlet();
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

        using var connection = _connectionService.CreateConnection();
        
        var (whereClause, parameters) = BuildWhereClause(tier, chainType, isActive, city, state, searchTerm, 
            minRank, maxRank, needsVisit, maxDaysSinceVisit, highPerforming, minAchievementPercentage);
        
        var orderClause = BuildOrderClause(sortBy, sortDirection);
        var skip = (pageNumber - 1) * pageSize;

        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            {whereClause}
            {orderClause}
            LIMIT {pageSize} OFFSET {skip}";

        var results = await connection.QueryAsync<OutletDataModel>(sql, parameters);
        return results.Select(r => r.ToOutlet());
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

        using var connection = _connectionService.CreateConnection();
        
        var (whereClause, parameters) = BuildWhereClause(tier, chainType, isActive, city, state, searchTerm, 
            minRank, maxRank, needsVisit, maxDaysSinceVisit, highPerforming, minAchievementPercentage);

        var sql = $"SELECT COUNT(*) FROM {_tableName} {whereClause}";

        return await connection.QuerySingleAsync<int>(sql, parameters);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Enumerable.Empty<Outlet>();

        _logger.LogDebug("Getting outlets by name: {Name}", name);
        
        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE Name LIKE ?
            ORDER BY Name";

        var results = await connection.QueryAsync<OutletDataModel>(sql, new { Name = $"%{name}%" });
        return results.Select(r => r.ToOutlet());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetByTierAsync(string tier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tier))
            return Enumerable.Empty<Outlet>();

        _logger.LogDebug("Getting outlets by tier: {Tier}", tier);
        
        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE Tier = ?
            ORDER BY Name";

        var results = await connection.QueryAsync<OutletDataModel>(sql, new { Tier = tier });
        return results.Select(r => r.ToOutlet());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetByChainTypeAsync(ChainType chainType, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting outlets by chain type: {ChainType}", chainType);
        
        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE ChainType = ?
            ORDER BY Name";

        var results = await connection.QueryAsync<OutletDataModel>(sql, new { ChainType = (int)chainType });
        return results.Select(r => r.ToOutlet());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Outlet>> GetActiveOutletsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting active outlets");
        
        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE IsActive = true
            ORDER BY Name";

        var results = await connection.QueryAsync<OutletDataModel>(sql);
        return results.Select(r => r.ToOutlet());
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

        using var connection = _connectionService.CreateConnection();
        var skip = (pageNumber - 1) * pageSize;
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE Name LIKE ? OR AddressStreet LIKE ? OR AddressCity LIKE ? OR AddressState LIKE ?
            ORDER BY CreatedAt DESC
            LIMIT {pageSize} OFFSET {skip}";

        var searchPattern = $"%{searchTerm}%";
        var results = await connection.QueryAsync<OutletDataModel>(sql, new { 
            SearchTerm1 = searchPattern,
            SearchTerm2 = searchPattern,
            SearchTerm3 = searchPattern,
            SearchTerm4 = searchPattern
        });
        
        return results.Select(r => r.ToOutlet());
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

        using var connection = _connectionService.CreateConnection();
        var skip = (pageNumber - 1) * pageSize;
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE Rank >= ? AND Rank <= ?
            ORDER BY Rank
            LIMIT {pageSize} OFFSET {skip}";

        var results = await connection.QueryAsync<OutletDataModel>(sql, new { MinRank = minRank, MaxRank = maxRank });
        return results.Select(r => r.ToOutlet());
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

        using var connection = _connectionService.CreateConnection();
        var cutoffDate = DateTime.UtcNow.AddDays(-maxDaysSinceVisit);
        var skip = (pageNumber - 1) * pageSize;
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE IsActive = true AND (LastVisitDate IS NULL OR LastVisitDate < ?)
            ORDER BY LastVisitDate ASC
            LIMIT {pageSize} OFFSET {skip}";

        var results = await connection.QueryAsync<OutletDataModel>(sql, new { CutoffDate = cutoffDate });
        return results.Select(r => r.ToOutlet());
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

        using var connection = _connectionService.CreateConnection();
        var skip = (pageNumber - 1) * pageSize;
        var sql = $@"
            SELECT 
                Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency,
                VolumeSoldKg, VolumeTargetKg, AddressStreet, AddressCity, AddressState,
                AddressZipCode, AddressCountry, IsActive, LastVisitDate, CreatedAt, UpdatedAt
            FROM {_tableName} 
            WHERE IsActive = true AND VolumeTargetKg > 0 AND (VolumeSoldKg / VolumeTargetKg * 100) >= ?
            ORDER BY (VolumeSoldKg / VolumeTargetKg) DESC
            LIMIT {pageSize} OFFSET {skip}";

        var results = await connection.QueryAsync<OutletDataModel>(sql, new { MinAchievement = minAchievementPercentage });
        return results.Select(r => r.ToOutlet());
    }

    /// <inheritdoc />
    public async Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        if (outlet == null)
            throw new ArgumentNullException(nameof(outlet));

        _logger.LogDebug("Adding new outlet: {OutletName}", outlet.Name);

        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            INSERT INTO {_tableName} 
            (Id, Name, Tier, Rank, ChainType, SalesAmount, SalesCurrency, VolumeSoldKg, VolumeTargetKg,
             AddressStreet, AddressCity, AddressState, AddressZipCode, AddressCountry, 
             IsActive, LastVisitDate, CreatedAt, UpdatedAt)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

        await connection.ExecuteAsync(sql, new
        {
            Id = outlet.Id.ToString(),
            outlet.Name,
            outlet.Tier,
            outlet.Rank,
            ChainType = (int)outlet.ChainType,
            SalesAmount = outlet.Sales.Amount,
            SalesCurrency = outlet.Sales.Currency,
            outlet.VolumeSoldKg,
            outlet.VolumeTargetKg,
            AddressStreet = outlet.Address.Street,
            AddressCity = outlet.Address.City,
            AddressState = outlet.Address.State,
            AddressZipCode = outlet.Address.PostalCode,
            AddressCountry = outlet.Address.Country,
            outlet.IsActive,
            outlet.LastVisitDate,
            outlet.CreatedAt,
            outlet.UpdatedAt
        });

        _logger.LogInformation("Successfully added outlet with ID: {OutletId}", outlet.Id);
        return outlet;
    }

    /// <inheritdoc />
    public async Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default)
    {
        if (outlet == null)
            throw new ArgumentNullException(nameof(outlet));

        _logger.LogDebug("Updating outlet: {OutletId}", outlet.Id);

        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            UPDATE {_tableName} SET
                Name = ?, Tier = ?, Rank = ?, ChainType = ?, SalesAmount = ?, SalesCurrency = ?,
                VolumeSoldKg = ?, VolumeTargetKg = ?, AddressStreet = ?, AddressCity = ?, AddressState = ?,
                AddressZipCode = ?, AddressCountry = ?, IsActive = ?, LastVisitDate = ?, UpdatedAt = ?
            WHERE Id = ?";

        var rowsAffected = await connection.ExecuteAsync(sql, new
        {
            outlet.Name,
            outlet.Tier,
            outlet.Rank,
            ChainType = (int)outlet.ChainType,
            SalesAmount = outlet.Sales.Amount,
            SalesCurrency = outlet.Sales.Currency,
            outlet.VolumeSoldKg,
            outlet.VolumeTargetKg,
            AddressStreet = outlet.Address.Street,
            AddressCity = outlet.Address.City,
            AddressState = outlet.Address.State,
            AddressZipCode = outlet.Address.PostalCode,
            AddressCountry = outlet.Address.Country,
            outlet.IsActive,
            outlet.LastVisitDate,
            outlet.UpdatedAt,
            Id = outlet.Id.ToString()
        });

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Outlet with ID {outlet.Id} not found for update");
        }

        _logger.LogInformation("Successfully updated outlet: {OutletId}", outlet.Id);
        return outlet;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Deleting outlet: {OutletId}", id);

        using var connection = _connectionService.CreateConnection();
        var sql = $"DELETE FROM {_tableName} WHERE Id = ?";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id.ToString() });

        if (rowsAffected == 0)
        {
            _logger.LogWarning("Outlet not found for deletion: {OutletId}", id);
            return false;
        }

        _logger.LogInformation("Successfully deleted outlet: {OutletId}", id);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionService.CreateConnection();
        var sql = $"SELECT COUNT(*) FROM {_tableName} WHERE Id = ?";
        var count = await connection.QuerySingleAsync<int>(sql, new { Id = id.ToString() });
        return count > 0;
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

        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            SELECT COUNT(*) FROM {_tableName} 
            WHERE Name = ? AND AddressCity = ? AND AddressState = ?";

        object parameters = new { Name = name, City = city, State = state };

        if (excludeId.HasValue)
        {
            sql += " AND Id != ?";
            parameters = new { Name = name, City = city, State = state, ExcludeId = excludeId.Value.ToString() };
        }

        var count = await connection.QuerySingleAsync<int>(sql, parameters);
        return count > 0;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetDistinctTiersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting distinct tiers");
        
        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            SELECT DISTINCT Tier 
            FROM {_tableName} 
            WHERE Tier IS NOT NULL AND Tier != ''
            ORDER BY Tier";

        return await connection.QueryAsync<string>(sql);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetDistinctCitiesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting distinct cities");
        
        using var connection = _connectionService.CreateConnection();
        var sql = $@"
            SELECT DISTINCT AddressCity 
            FROM {_tableName} 
            WHERE AddressCity IS NOT NULL AND AddressCity != ''
            ORDER BY AddressCity";

        return await connection.QueryAsync<string>(sql);
    }

    /// <summary>
    /// Builds WHERE clause and parameters for filtering
    /// </summary>
    private (string whereClause, object parameters) BuildWhereClause(
        string? tier, ChainType? chainType, bool? isActive, string? city, string? state, string? searchTerm,
        int? minRank, int? maxRank, bool? needsVisit, int maxDaysSinceVisit, bool? highPerforming, decimal minAchievementPercentage)
    {
        var conditions = new List<string>();
        var paramDict = new Dictionary<string, object>();

        if (!string.IsNullOrWhiteSpace(tier))
        {
            conditions.Add("Tier = @Tier");
            paramDict["Tier"] = tier;
        }

        if (chainType.HasValue)
        {
            conditions.Add("ChainType = @ChainType");
            paramDict["ChainType"] = (int)chainType.Value;
        }

        if (isActive.HasValue)
        {
            conditions.Add("IsActive = @IsActive");
            paramDict["IsActive"] = isActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            conditions.Add("AddressCity = @City");
            paramDict["City"] = city;
        }

        if (!string.IsNullOrWhiteSpace(state))
        {
            conditions.Add("AddressState = @State");
            paramDict["State"] = state;
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            conditions.Add("(Name LIKE @SearchTerm OR AddressStreet LIKE @SearchTerm OR AddressCity LIKE @SearchTerm OR AddressState LIKE @SearchTerm)");
            paramDict["SearchTerm"] = $"%{searchTerm}%";
        }

        if (minRank.HasValue)
        {
            conditions.Add("Rank >= @MinRank");
            paramDict["MinRank"] = minRank.Value;
        }

        if (maxRank.HasValue)
        {
            conditions.Add("Rank <= @MaxRank");
            paramDict["MaxRank"] = maxRank.Value;
        }

        if (needsVisit == true)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-maxDaysSinceVisit);
            conditions.Add("IsActive = true AND (LastVisitDate IS NULL OR LastVisitDate < @CutoffDate)");
            paramDict["CutoffDate"] = cutoffDate;
        }

        if (highPerforming == true)
        {
            conditions.Add("IsActive = true AND VolumeTargetKg > 0 AND (VolumeSoldKg / VolumeTargetKg * 100) >= @MinAchievement");
            paramDict["MinAchievement"] = minAchievementPercentage;
        }

        var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
        return (whereClause, paramDict);
    }

    /// <summary>
    /// Builds ORDER BY clause for sorting
    /// </summary>
    private string BuildOrderClause(string sortBy, string sortDirection)
    {
        var direction = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

        var column = sortBy.ToLowerInvariant() switch
        {
            "name" => "Name",
            "tier" => "Tier",
            "rank" => "Rank",
            "chaintype" => "ChainType",
            "sales" => "SalesAmount",
            "volumesold" => "VolumeSoldKg",
            "volumetarget" => "VolumeTargetKg",
            "targetachievement" => "(CASE WHEN VolumeTargetKg > 0 THEN (VolumeSoldKg / VolumeTargetKg * 100) ELSE 0 END)",
            "lastvisitdate" => "LastVisitDate",
            "city" => "AddressCity",
            "state" => "AddressState",
            "updatedat" => "UpdatedAt",
            _ => "CreatedAt"
        };

        return $"ORDER BY {column} {direction}";
    }
}

/// <summary>
/// Data model for mapping Databricks query results to Outlet entities
/// </summary>
public class OutletDataModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public int Rank { get; set; }
    public int ChainType { get; set; }
    public decimal SalesAmount { get; set; }
    public string SalesCurrency { get; set; } = string.Empty;
    public decimal VolumeSoldKg { get; set; }
    public decimal VolumeTargetKg { get; set; }
    public string AddressStreet { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string AddressState { get; set; } = string.Empty;
    public string AddressZipCode { get; set; } = string.Empty;
    public string AddressCountry { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastVisitDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Converts the data model to an Outlet entity
    /// </summary>
    /// <returns>Outlet entity</returns>
    public Outlet ToOutlet()
    {
        var address = new Address(AddressStreet, AddressCity, AddressState, AddressZipCode, AddressCountry);
        
        // Create outlet with basic constructor
        var outlet = new Outlet(Name, Tier, Rank, (ChainType)ChainType, address);
        
        // Use reflection to set private fields that can't be set through constructor
        var outletType = typeof(Outlet);
        
        // Set Id
        var idProperty = outletType.GetProperty("Id");
        idProperty?.SetValue(outlet, Guid.Parse(Id));
        
        // Set Sales
        var money = new Money(SalesAmount, SalesCurrency);
        outlet.UpdateSales(money, "system");
        
        // Set volume data
        outlet.UpdateVolumeSold(VolumeSoldKg, "system");
        outlet.UpdateVolumeTarget(VolumeTargetKg, "system");
        
        // Set active status
        if (!IsActive)
        {
            outlet.Deactivate("system");
        }
        
        // Set visit date
        if (LastVisitDate.HasValue)
        {
            outlet.RecordVisit(LastVisitDate.Value, "system");
        }
        
        // Set audit fields using reflection
        var createdAtField = outletType.BaseType?.GetProperty("CreatedAt");
        var updatedAtField = outletType.BaseType?.GetProperty("UpdatedAt");
        
        createdAtField?.SetValue(outlet, CreatedAt);
        updatedAtField?.SetValue(outlet, UpdatedAt);
        
        return outlet;
    }
}