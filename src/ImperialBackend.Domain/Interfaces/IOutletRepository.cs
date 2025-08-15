using ImperialBackend.Domain.Entities;

namespace ImperialBackend.Domain.Interfaces;

public interface IOutletRepository
{
    Task<Outlet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Outlet>> GetAllAsync(
        int? year = null,
        int? week = null,
        string? healthStatus = null,
        string? searchTerm = null,
        int pageNumber = 1,
        int pageSize = 10,
        string sortBy = "StoreRank",
        string sortDirection = "asc",
        CancellationToken cancellationToken = default);

    Task<int> GetCountAsync(
        int? year = null,
        int? week = null,
        string? healthStatus = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default);

    Task<Outlet> AddAsync(Outlet outlet, CancellationToken cancellationToken = default);
    Task<Outlet> UpdateAsync(Outlet outlet, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}