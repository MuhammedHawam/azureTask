using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Enums;

namespace ImperialBackend.Domain.Interfaces;

/// <summary>
/// Repository interface for Outlet entity operations
/// </summary>
public interface IOutletRepository
{
    /// <summary>
    /// Gets an outlet by its unique identifier
    /// </summary>
    /// <param name="id">The outlet identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The outlet if found, null otherwise</returns>
    Task<Outlet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outlets by name (partial match)
    /// </summary>
    /// <param name="name">The outlet name to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of outlets matching the name</returns>
    Task<IEnumerable<Outlet>> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all outlets with optional filtering and pagination
    /// </summary>
    /// <param name="tier">Optional tier filter</param>
    /// <param name="chainType">Optional chain type filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="city">Optional city filter</param>
    /// <param name="state">Optional state filter</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of outlets</returns>
    Task<IEnumerable<Outlet>> GetAllAsync(
        string? tier = null,
        ChainType? chainType = null,
        bool? isActive = null,
        string? city = null,
        string? state = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of outlets with optional filtering
    /// </summary>
    /// <param name="tier">Optional tier filter</param>
    /// <param name="chainType">Optional chain type filter</param>
    /// <param name="isActive">Optional active status filter</param>
    /// <param name="city">Optional city filter</param>
    /// <param name="state">Optional state filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The total count of outlets</returns>
    Task<int> GetCountAsync(
        string? tier = null,
        ChainType? chainType = null,
        bool? isActive = null,
        string? city = null,
        string? state = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches outlets by name or address
    /// </summary>
    /// <param name="searchTerm">The search term</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of outlets matching the search term</returns>
    Task<IEnumerable<Outlet>> SearchAsync(
        string searchTerm,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outlets that need visits (haven't been visited for more than specified days)
    /// </summary>
    /// <param name="maxDaysSinceVisit">Maximum days since last visit</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of outlets that need visits</returns>
    Task<IEnumerable<Outlet>> GetOutletsNeedingVisitAsync(
        int maxDaysSinceVisit = 30,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outlets by rank range
    /// </summary>
    /// <param name="minRank">Minimum rank (inclusive)</param>
    /// <param name="maxRank">Maximum rank (inclusive)</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of outlets within the rank range</returns>
    Task<IEnumerable<Outlet>> GetByRankRangeAsync(
        int minRank,
        int maxRank,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct tiers
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of tiers</returns>
    Task<IEnumerable<string>> GetTiersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all distinct cities
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of cities</returns>
    Task<IEnumerable<string>> GetCitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outlets with target achievement above specified percentage
    /// </summary>
    /// <param name="minAchievementPercentage">Minimum achievement percentage</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of high-performing outlets</returns>
    Task<IEnumerable<Outlet>> GetHighPerformingOutletsAsync(
        decimal minAchievementPercentage = 100,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new outlet
    /// </summary>
    /// <param name="outlet">The outlet to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The added outlet</returns>
    Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing outlet
    /// </summary>
    /// <param name="outlet">The outlet to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated outlet</returns>
    Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an outlet
    /// </summary>
    /// <param name="id">The outlet identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the outlet was deleted, false if not found</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an outlet exists with the given name and address
    /// </summary>
    /// <param name="name">The outlet name</param>
    /// <param name="city">The city</param>
    /// <param name="state">The state</param>
    /// <param name="excludeId">Optional outlet ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if an outlet exists, false otherwise</returns>
    Task<bool> ExistsWithNameAndLocationAsync(
        string name, 
        string city, 
        string state, 
        Guid? excludeId = null, 
        CancellationToken cancellationToken = default);
}