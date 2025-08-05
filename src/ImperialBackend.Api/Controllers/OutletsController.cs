using AutoMapper;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Application.Outlets.Commands.CreateOutlet;
using ImperialBackend.Application.Outlets.Queries.GetOutlets;
using ImperialBackend.Domain.Enums;
using ImperialBackend.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ImperialBackend.Api.Controllers;

/// <summary>
/// Controller for managing outlets with comprehensive filtering and sorting
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

    // Add repository dependency for tiers and cities endpoints
    private readonly IOutletRepository _outletRepository;

    /// <summary>
    /// Initializes a new instance of the OutletsController class
    /// </summary>
    /// <param name="mediator">The MediatR instance</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="logger">The logger</param>
    /// <param name="outletRepository">The outlet repository</param>
    public OutletsController(IMediator mediator, IMapper mapper, ILogger<OutletsController> logger, IOutletRepository outletRepository)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
    }

    /// <summary>
    /// Gets outlets with comprehensive filtering, sorting, and pagination
    /// </summary>
    /// <param name="query">The query parameters for filtering, sorting, and pagination</param>
    /// <returns>A paginated list of outlets</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OutletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<OutletDto>>> GetOutlets([FromQuery] GetOutletsQuery query)
    {
        try
        {
            _logger.LogInformation("Getting outlets with filters - Page: {PageNumber}, PageSize: {PageSize}, SortBy: {SortBy}", 
                query.PageNumber, query.PageSize, query.SortBy);

            // Validate pagination parameters
            if (query.PageNumber < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to get outlets: {Error}", result.Error);
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting outlets");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving outlets");
        }
    }

    /// <summary>
    /// Gets an outlet by ID
    /// </summary>
    /// <param name="id">The outlet ID</param>
    /// <returns>The outlet if found</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OutletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OutletDto>> GetOutlet(Guid id)
    {
        try
        {
            _logger.LogInformation("Getting outlet by ID: {OutletId}", id);

            var query = new GetOutletByIdQuery { Id = id };
            var result = await _mediator.Send(query);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    _logger.LogWarning("Outlet not found: {OutletId}", id);
                    return NotFound($"Outlet with ID {id} not found");
                }

                _logger.LogWarning("Failed to get outlet {OutletId}: {Error}", id, result.Error);
                return BadRequest(result.Error);
            }

            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting outlet {OutletId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the outlet");
        }
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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Gets high-performing outlets
    /// </summary>
    /// <param name="minAchievementPercentage">Minimum achievement percentage (default: 80)</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <returns>Paged list of high-performing outlets</returns>
    [HttpGet("high-performing")]
    [ProducesResponseType(typeof(PagedResult<OutletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<OutletDto>>> GetHighPerformingOutlets(
        [FromQuery] decimal minAchievementPercentage = 80.0m,
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

        if (!result.IsSuccess)
        {
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
    public async Task<ActionResult<IEnumerable<string>>> GetTiers()
    {
        var tiers = await _outletRepository.GetDistinctTiersAsync();
        return Ok(tiers);
    }

    /// <summary>
    /// Gets all outlet cities
    /// </summary>
    /// <returns>List of cities</returns>
    [HttpGet("cities")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<string>>> GetCities()
    {
        var cities = await _outletRepository.GetDistinctCitiesAsync();
        return Ok(cities);
    }

    /// <summary>
    /// Creates a new outlet
    /// </summary>
    /// <param name="command">The outlet creation data</param>
    /// <returns>The created outlet</returns>
    [HttpPost]
    [ProducesResponseType(typeof(OutletDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OutletDto>> CreateOutlet([FromBody] CreateOutletCommand command)
    {
        try
        {
            _logger.LogInformation("Creating new outlet: {OutletName}", command.Name);

            // Create a new command with the user ID set
            var commandWithUserId = command with { UserId = GetCurrentUserId() };

            var result = await _mediator.Send(commandWithUserId);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to create outlet: {Error}", result.Error);
                return BadRequest(result.Error);
            }

            _logger.LogInformation("Successfully created outlet with ID: {OutletId}", result.Value?.Id);
            return CreatedAtAction(nameof(GetOutlet), new { id = result.Value?.Id }, result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating outlet");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the outlet");
        }
    }

    /// <summary>
    /// Updates an existing outlet
    /// </summary>
    /// <param name="id">The outlet ID</param>
    /// <param name="command">The outlet update data</param>
    /// <returns>The updated outlet</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(OutletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<OutletDto>> UpdateOutlet(Guid id, [FromBody] UpdateOutletCommand command)
    {
        try
        {
            if (id != command.Id)
            {
                return BadRequest("URL ID does not match the command ID");
            }

            _logger.LogInformation("Updating outlet: {OutletId}", id);

            // Set the user ID from the current user's claims
            command.UserId = GetCurrentUserId();

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    _logger.LogWarning("Outlet not found for update: {OutletId}", id);
                    return NotFound($"Outlet with ID {id} not found");
                }

                _logger.LogWarning("Failed to update outlet {OutletId}: {Error}", id, result.Error);
                return BadRequest(result.Error);
            }

            _logger.LogInformation("Successfully updated outlet: {OutletId}", id);
            return Ok(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating outlet {OutletId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the outlet");
        }
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
    public async Task<IActionResult> DeleteOutlet(Guid id)
    {
        try
        {
            _logger.LogInformation("Deleting outlet: {OutletId}", id);

            var command = new DeleteOutletCommand 
            { 
                Id = id,
                UserId = GetCurrentUserId()
            };

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    _logger.LogWarning("Outlet not found for deletion: {OutletId}", id);
                    return NotFound($"Outlet with ID {id} not found");
                }

                _logger.LogWarning("Failed to delete outlet {OutletId}: {Error}", id, result.Error);
                return BadRequest(result.Error);
            }

            _logger.LogInformation("Successfully deleted outlet: {OutletId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting outlet {OutletId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the outlet");
        }
    }

    /// <summary>
    /// Records a visit to an outlet
    /// </summary>
    /// <param name="id">The outlet ID</param>
    /// <param name="command">The visit recording data</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/visit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecordVisit(Guid id, [FromBody] RecordVisitCommand command)
    {
        try
        {
            if (id != command.Id)
            {
                return BadRequest("URL ID does not match the command ID");
            }

            _logger.LogInformation("Recording visit for outlet: {OutletId}", id);

            // Set the user ID from the current user's claims
            command.UserId = GetCurrentUserId();

            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found") == true)
                {
                    _logger.LogWarning("Outlet not found for visit recording: {OutletId}", id);
                    return NotFound($"Outlet with ID {id} not found");
                }

                _logger.LogWarning("Failed to record visit for outlet {OutletId}: {Error}", id, result.Error);
                return BadRequest(result.Error);
            }

            _logger.LogInformation("Successfully recorded visit for outlet: {OutletId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while recording visit for outlet {OutletId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while recording the visit");
        }
    }

    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    /// <returns>The current user ID</returns>
    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ?? 
               User.FindFirst("oid")?.Value ?? 
               "system";
    }


}

// Command classes for the controller
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
    public string Currency { get; init; } = string.Empty;
    public decimal VolumeSoldKg { get; init; }
    public decimal VolumeTargetKg { get; init; }
    public AddressDto Address { get; init; } = null!;
    public DateTime? LastVisitDate { get; init; }
    public string UserId { get; set; } = string.Empty;
}

public record DeleteOutletCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string UserId { get; set; } = string.Empty;
}

public record RecordVisitCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public DateTime VisitDate { get; init; }
    public string UserId { get; set; } = string.Empty;
}

// Simple handlers for the controller commands
public class GetOutletByIdQueryHandler : IRequestHandler<GetOutletByIdQuery, Result<OutletDto>>
{
    private readonly IOutletRepository _repository;
    private readonly IMapper _mapper;

    public GetOutletByIdQueryHandler(IOutletRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<OutletDto>> Handle(GetOutletByIdQuery request, CancellationToken cancellationToken)
    {
        var outlet = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (outlet == null)
        {
            return Result<OutletDto>.Failure("Outlet not found");
        }

        var dto = _mapper.Map<OutletDto>(outlet);
        return Result<OutletDto>.Success(dto);
    }
}

public class UpdateOutletCommandHandler : IRequestHandler<UpdateOutletCommand, Result<OutletDto>>
{
    private readonly IOutletRepository _repository;
    private readonly IMapper _mapper;

    public UpdateOutletCommandHandler(IOutletRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<Result<OutletDto>> Handle(UpdateOutletCommand request, CancellationToken cancellationToken)
    {
        var outlet = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (outlet == null)
        {
            return Result<OutletDto>.Failure("Outlet not found");
        }

        // Update outlet properties using available methods
        outlet.UpdateSales(new ImperialBackend.Domain.ValueObjects.Money(request.Sales, request.Currency), request.UserId);
        outlet.UpdateVolumeSold(request.VolumeSoldKg, request.UserId);
        outlet.UpdateVolumeTarget(request.VolumeTargetKg, request.UserId);
        
        if (request.LastVisitDate.HasValue)
        {
            outlet.RecordVisit(request.LastVisitDate.Value, request.UserId);
        }

        var updatedOutlet = await _repository.UpdateAsync(outlet, cancellationToken);
        var dto = _mapper.Map<OutletDto>(updatedOutlet);
        return Result<OutletDto>.Success(dto);
    }
}

public class DeleteOutletCommandHandler : IRequestHandler<DeleteOutletCommand, Result>
{
    private readonly IOutletRepository _repository;

    public DeleteOutletCommandHandler(IOutletRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(DeleteOutletCommand request, CancellationToken cancellationToken)
    {
        var success = await _repository.DeleteAsync(request.Id, cancellationToken);
        return success ? Result.Success() : Result.Failure("Outlet not found");
    }
}

public class RecordVisitCommandHandler : IRequestHandler<RecordVisitCommand, Result>
{
    private readonly IOutletRepository _repository;

    public RecordVisitCommandHandler(IOutletRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(RecordVisitCommand request, CancellationToken cancellationToken)
    {
        var outlet = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (outlet == null)
        {
            return Result.Failure("Outlet not found");
        }

        outlet.RecordVisit(request.VisitDate, request.UserId);
        await _repository.UpdateAsync(outlet, cancellationToken);
        return Result.Success();
    }
}