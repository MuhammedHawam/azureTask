using AutoMapper;
using AzureProductApi.Application.Common.Models;
using AzureProductApi.Application.DTOs;
using AzureProductApi.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AzureProductApi.Application.Outlets.Queries.GetOutlets;

/// <summary>
/// Handler for GetOutletsQuery
/// </summary>
public class GetOutletsQueryHandler : IRequestHandler<GetOutletsQuery, Result<PagedResult<OutletDto>>>
{
    private readonly IOutletRepository _outletRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetOutletsQueryHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the GetOutletsQueryHandler class
    /// </summary>
    /// <param name="outletRepository">The outlet repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="logger">The logger</param>
    public GetOutletsQueryHandler(
        IOutletRepository outletRepository,
        IMapper mapper,
        ILogger<GetOutletsQueryHandler> logger)
    {
        _outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the GetOutletsQuery
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the paged outlets or error information</returns>
    public async Task<Result<PagedResult<OutletDto>>> Handle(GetOutletsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting outlets - Page: {PageNumber}, PageSize: {PageSize}, Filters: {@Filters}",
                request.PageNumber, request.PageSize, new
                {
                    request.Tier,
                    request.ChainType,
                    request.IsActive,
                    request.City,
                    request.State,
                    request.SearchTerm,
                    request.MinRank,
                    request.MaxRank,
                    request.NeedsVisit,
                    request.HighPerforming
                });

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return Result<PagedResult<OutletDto>>.Failure("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<PagedResult<OutletDto>>.Failure("Page size must be between 1 and 100");
            }

            IEnumerable<Domain.Entities.Outlet> outlets;
            int totalCount;

            // Handle special query types first
            if (request.NeedsVisit == true)
            {
                outlets = await _outletRepository.GetOutletsNeedingVisitAsync(
                    request.MaxDaysSinceVisit,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                // Get total count for outlets needing visit
                var allNeedingVisit = await _outletRepository.GetOutletsNeedingVisitAsync(
                    request.MaxDaysSinceVisit,
                    1,
                    int.MaxValue,
                    cancellationToken);
                totalCount = allNeedingVisit.Count();
            }
            else if (request.HighPerforming == true)
            {
                outlets = await _outletRepository.GetHighPerformingOutletsAsync(
                    request.MinAchievementPercentage,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                // Get total count for high-performing outlets
                var allHighPerforming = await _outletRepository.GetHighPerformingOutletsAsync(
                    request.MinAchievementPercentage,
                    1,
                    int.MaxValue,
                    cancellationToken);
                totalCount = allHighPerforming.Count();
            }
            else if (request.MinRank.HasValue && request.MaxRank.HasValue)
            {
                outlets = await _outletRepository.GetByRankRangeAsync(
                    request.MinRank.Value,
                    request.MaxRank.Value,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                // Get total count for rank range
                var allInRankRange = await _outletRepository.GetByRankRangeAsync(
                    request.MinRank.Value,
                    request.MaxRank.Value,
                    1,
                    int.MaxValue,
                    cancellationToken);
                totalCount = allInRankRange.Count();
            }
            else if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                outlets = await _outletRepository.SearchAsync(
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                // Get total count for search
                var allSearchResults = await _outletRepository.SearchAsync(
                    request.SearchTerm,
                    1,
                    int.MaxValue,
                    cancellationToken);
                totalCount = allSearchResults.Count();
            }
            else
            {
                // Standard filtered query
                outlets = await _outletRepository.GetAllAsync(
                    request.Tier,
                    request.ChainType,
                    request.IsActive,
                    request.City,
                    request.State,
                    request.PageNumber,
                    request.PageSize,
                    cancellationToken);

                // Get total count for standard filters
                totalCount = await _outletRepository.GetCountAsync(
                    request.Tier,
                    request.ChainType,
                    request.IsActive,
                    request.City,
                    request.State,
                    cancellationToken);
            }

            // Map to DTOs
            var outletDtos = _mapper.Map<List<OutletDto>>(outlets);

            // Apply sorting if needed (this could be moved to repository for better performance)
            outletDtos = ApplySorting(outletDtos, request.SortBy, request.SortDirection);

            // Create paged result
            var pagedResult = new PagedResult<OutletDto>(
                outletDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation("Successfully retrieved {Count} outlets out of {TotalCount} total",
                pagedResult.Count, pagedResult.TotalCount);

            return Result<PagedResult<OutletDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting outlets");
            return Result<PagedResult<OutletDto>>.Failure("An error occurred while retrieving outlets");
        }
    }

    private static List<OutletDto> ApplySorting(List<OutletDto> outlets, string sortBy, string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "name" => isDescending
                ? outlets.OrderByDescending(o => o.Name).ToList()
                : outlets.OrderBy(o => o.Name).ToList(),
            "tier" => isDescending
                ? outlets.OrderByDescending(o => o.Tier).ToList()
                : outlets.OrderBy(o => o.Tier).ToList(),
            "rank" => isDescending
                ? outlets.OrderByDescending(o => o.Rank).ToList()
                : outlets.OrderBy(o => o.Rank).ToList(),
            "chaintype" => isDescending
                ? outlets.OrderByDescending(o => o.ChainType).ToList()
                : outlets.OrderBy(o => o.ChainType).ToList(),
            "sales" => isDescending
                ? outlets.OrderByDescending(o => o.Sales).ToList()
                : outlets.OrderBy(o => o.Sales).ToList(),
            "volumesold" => isDescending
                ? outlets.OrderByDescending(o => o.VolumeSoldKg).ToList()
                : outlets.OrderBy(o => o.VolumeSoldKg).ToList(),
            "volumetarget" => isDescending
                ? outlets.OrderByDescending(o => o.VolumeTargetKg).ToList()
                : outlets.OrderBy(o => o.VolumeTargetKg).ToList(),
            "targetachievement" => isDescending
                ? outlets.OrderByDescending(o => o.TargetAchievementPercentage).ToList()
                : outlets.OrderBy(o => o.TargetAchievementPercentage).ToList(),
            "lastvisitdate" => isDescending
                ? outlets.OrderByDescending(o => o.LastVisitDate).ToList()
                : outlets.OrderBy(o => o.LastVisitDate).ToList(),
            "city" => isDescending
                ? outlets.OrderByDescending(o => o.Address.City).ToList()
                : outlets.OrderBy(o => o.Address.City).ToList(),
            "state" => isDescending
                ? outlets.OrderByDescending(o => o.Address.State).ToList()
                : outlets.OrderBy(o => o.Address.State).ToList(),
            "updatedat" => isDescending
                ? outlets.OrderByDescending(o => o.UpdatedAt).ToList()
                : outlets.OrderBy(o => o.UpdatedAt).ToList(),
            _ => isDescending
                ? outlets.OrderByDescending(o => o.CreatedAt).ToList()
                : outlets.OrderBy(o => o.CreatedAt).ToList()
        };
    }
}