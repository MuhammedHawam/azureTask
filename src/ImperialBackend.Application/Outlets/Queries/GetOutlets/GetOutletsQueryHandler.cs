using AutoMapper;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Application.Outlets.Queries.GetOutlets;

/// <summary>
/// Handler for GetOutletsQuery with optimized performance
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
    /// Handles the GetOutletsQuery request
    /// </summary>
    /// <param name="request">The query request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A paged result of outlet DTOs</returns>
    public async Task<Result<PagedResult<OutletDto>>> Handle(GetOutletsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Handling GetOutletsQuery - Page: {PageNumber}, PageSize: {PageSize}",
                request.PageNumber, request.PageSize);

            // Validate pagination parameters
            if (request.PageNumber < 1)
            {
                return Result<PagedResult<OutletDto>>.Failure("Page number must be greater than 0");
            }

            if (request.PageSize < 1 || request.PageSize > 100)
            {
                return Result<PagedResult<OutletDto>>.Failure("Page size must be between 1 and 100");
            }

            // Get outlets with pagination
            var outlets = await _outletRepository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            // Get total count for pagination
            var totalCount = await _outletRepository.GetCountAsync(cancellationToken);

            // Map to DTOs
            var outletDtos = _mapper.Map<IEnumerable<OutletDto>>(outlets);

            // Create paged result using constructor
            var pagedResult = new PagedResult<OutletDto>(
                outletDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogDebug("Successfully retrieved {Count} outlets (Page {PageNumber}/{TotalPages})",
                outletDtos.Count(), request.PageNumber, pagedResult.TotalPages);

            return Result<PagedResult<OutletDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling GetOutletsQuery");
            return Result<PagedResult<OutletDto>>.Failure("An error occurred while retrieving outlets");
        }
    }
}