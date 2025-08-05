using AutoMapper;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Application.Outlets.Commands.CreateOutlet;
using ImperialBackend.Application.Outlets.Queries.GetOutlets;
using ImperialBackend.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ImperialBackend.Api.Controllers;

/// <summary>
/// Controller for managing outlets
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OutletsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<OutletsController> _logger;

    /// <summary>
    /// Initializes a new instance of the OutletsController class
    /// </summary>
    /// <param name="mediator">The MediatR instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="logger">The logger</param>
    public OutletsController(IMediator mediator, IMapper mapper, ILogger<OutletsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets outlets with optional filtering and pagination
    /// </summary>
    /// <param name="tier">Filter by tier</param>
    /// <param name="chainType">Filter by chain type (1=Regional, 2=National)</param>
    /// <param name="isActive">Filter by active status</param>
    /// <param name="city">Filter by city</param>
    /// <param name="state">Filter by state</param>
    /// <param name="searchTerm">Search in name and address</param>
    /// <param name="minRank">Minimum rank filter</param>
    /// <param name="maxRank">Maximum rank filter</param>
    /// <param name="needsVisit">Filter outlets needing visits</param>
    /// <param name="maxDaysSinceVisit">Maximum days since visit for filtering outlets that need visits</param>
    /// <param name="highPerforming">Filter high-performing outlets</param>
    /// <param name="minAchievementPercentage">Minimum achievement percentage for high-performing outlets</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortDirection">Sort direction (asc/desc)</param>
    /// <returns>Paged list of outlets</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OutletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<OutletDto>>> GetOutlets(
        [FromQuery] string? tier = null,
        [FromQuery] ChainType? chainType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? city = null,
        [FromQuery] string? state = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int? minRank = null,
        [FromQuery] int? maxRank = null,
        [FromQuery] bool? needsVisit = null,
        [FromQuery] int maxDaysSinceVisit = 30,
        [FromQuery] bool? highPerforming = null,
        [FromQuery] decimal minAchievementPercentage = 100,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortDirection = "desc")
    {
        var query = new GetOutletsQuery
        {
            Tier = tier,
            ChainType = chainType,
            IsActive = isActive,
            City = city,
            State = state,
            SearchTerm = searchTerm,
            MinRank = minRank,
            MaxRank = maxRank,
            NeedsVisit = needsVisit,
            MaxDaysSinceVisit = maxDaysSinceVisit,
            HighPerforming = highPerforming,
            MinAchievementPercentage = minAchievementPercentage,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets an outlet by ID
    /// </summary>
    /// <param name="id">The outlet ID</param>
    /// <returns>The outlet details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OutletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OutletDto>> GetOutlet(Guid id)
    {
        var query = new GetOutletByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return NotFound(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new outlet
    /// </summary>
    /// <param name="createOutletDto">The outlet creation data</param>
    /// <returns>The created outlet</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OutletDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OutletDto>> CreateOutlet([FromBody] CreateOutletDto createOutletDto)
    {
        var userId = GetCurrentUserId();
        var command = _mapper.Map<CreateOutletCommand>(createOutletDto);
        command = command with { UserId = userId };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error?.Contains("already exists") == true)
            {
                return Conflict(result.Error);
            }
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetOutlet), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Updates an existing outlet
    /// </summary>
    /// <param name="id">The outlet ID</param>
    /// <param name="updateOutletDto">The outlet update data</param>
    /// <returns>The updated outlet</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OutletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OutletDto>> UpdateOutlet(Guid id, [FromBody] UpdateOutletDto updateOutletDto)
    {
        var userId = GetCurrentUserId();
        var command = new UpdateOutletCommand
        {
            Id = id,
            Name = updateOutletDto.Name,
            Tier = updateOutletDto.Tier,
            Rank = updateOutletDto.Rank,
            ChainType = updateOutletDto.ChainType,
            Sales = updateOutletDto.Sales,
            Currency = updateOutletDto.Currency,
            VolumeSoldKg = updateOutletDto.VolumeSoldKg,
            VolumeTargetKg = updateOutletDto.VolumeTargetKg,
            Address = updateOutletDto.Address,
            LastVisitDate = updateOutletDto.LastVisitDate,
            UserId = userId
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Deletes an outlet
    /// </summary>
    /// <param name="id">The outlet ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteOutlet(Guid id)
    {
        var userId = GetCurrentUserId();
        var command = new DeleteOutletCommand { Id = id, UserId = userId };
        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Records a visit to an outlet
    /// </summary>
    /// <param name="id">The outlet ID</param>
    /// <param name="visitDate">The visit date (optional, defaults to current date)</param>
    /// <returns>The updated outlet</returns>
    [HttpPost("{id:guid}/visit")]
    [ProducesResponseType(typeof(OutletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OutletDto>> RecordVisit(Guid id, [FromBody] DateTime? visitDate = null)
    {
        var userId = GetCurrentUserId();
        var command = new RecordVisitCommand
        {
            Id = id,
            VisitDate = visitDate ?? DateTime.UtcNow,
            UserId = userId
        };

        var result = await _mediator.Send(command);

        if (result.IsFailure)
        {
            if (result.Error?.Contains("not found") == true)
            {
                return NotFound(result.Error);
            }
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all outlet tiers
    /// </summary>
    /// <returns>List of tiers</returns>
    [HttpGet("tiers")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetTiers()
    {
        var query = new GetTiersQuery();
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets all outlet cities
    /// </summary>
    /// <returns>List of cities</returns>
    [HttpGet("cities")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<string>>> GetCities()
    {
        var query = new GetCitiesQuery();
        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets outlets that need visits
    /// </summary>
    /// <param name="maxDaysSinceVisit">Maximum days since last visit (default: 30)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paged list of outlets needing visits</returns>
    [HttpGet("needing-visit")]
    [ProducesResponseType(typeof(PagedResult<OutletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<OutletDto>>> GetOutletsNeedingVisit(
        [FromQuery] int maxDaysSinceVisit = 30,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetOutletsQuery
        {
            NeedsVisit = true,
            MaxDaysSinceVisit = maxDaysSinceVisit,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = "LastVisitDate",
            SortDirection = "asc"
        };

        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets high-performing outlets
    /// </summary>
    /// <param name="minAchievementPercentage">Minimum achievement percentage (default: 100)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paged list of high-performing outlets</returns>
    [HttpGet("high-performing")]
    [ProducesResponseType(typeof(PagedResult<OutletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<OutletDto>>> GetHighPerformingOutlets(
        [FromQuery] decimal minAchievementPercentage = 100,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetOutletsQuery
        {
            HighPerforming = true,
            MinAchievementPercentage = minAchievementPercentage,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SortBy = "TargetAchievement",
            SortDirection = "desc"
        };

        var result = await _mediator.Send(query);

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("oid")?.Value ?? 
               "system";
    }
}

// Additional command and query classes needed for the controller
public record GetOutletByIdQuery : IRequest<Result<OutletDto>>
{
    public Guid Id { get; init; }
}

public record UpdateOutletCommand : IRequest<Result<OutletDto>>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public int Rank { get; init; }
    public ChainType ChainType { get; init; }
    public decimal Sales { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal VolumeSoldKg { get; init; }
    public decimal VolumeTargetKg { get; init; }
    public AddressDto Address { get; init; } = new();
    public DateTime? LastVisitDate { get; init; }
    public string UserId { get; init; } = string.Empty;
}

public record DeleteOutletCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = string.Empty;
}

public record RecordVisitCommand : IRequest<Result<OutletDto>>
{
    public Guid Id { get; init; }
    public DateTime VisitDate { get; init; }
    public string UserId { get; init; } = string.Empty;
}

public record GetTiersQuery : IRequest<Result<IEnumerable<string>>>;

public record GetCitiesQuery : IRequest<Result<IEnumerable<string>>>;