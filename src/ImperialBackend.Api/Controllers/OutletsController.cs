using AutoMapper;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Application.Outlets.Commands.CreateOutlet;
using ImperialBackend.Application.Outlets.Queries.GetOutlets;
using ImperialBackend.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ImperialBackend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class OutletsController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly IMapper _mapper;
	private readonly ILogger<OutletsController> _logger;
	private readonly IOutletRepository _outletRepository;

	public OutletsController(IMediator mediator, IMapper mapper, ILogger<OutletsController> logger, IOutletRepository outletRepository)
	{
		_mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
		_mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
	}

	// GET: api/Outlets
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

	// GET: api/Outlets/{outletIdentifier}
	[HttpGet("{outletIdentifier}")]
	[ProducesResponseType(typeof(OutletDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<OutletDto>> GetOutlet(string outletIdentifier)
	{
		try
		{
			_logger.LogInformation("Getting outlet by identifier: {OutletIdentifier}", outletIdentifier);

			var outlet = await _outletRepository.GetByIdAsync(outletIdentifier);
			if (outlet == null)
			{
				_logger.LogWarning("Outlet not found: {OutletIdentifier}", outletIdentifier);
				return NotFound($"Outlet with identifier {outletIdentifier} not found");
			}

			var dto = _mapper.Map<OutletDto>(outlet);
			return Ok(dto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while getting outlet {OutletIdentifier}", outletIdentifier);
			return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the outlet");
		}
	}

	// POST: api/Outlets
	[HttpPost]
	[ProducesResponseType(typeof(OutletDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<ActionResult<OutletDto>> CreateOutlet([FromBody] CreateOutletCommand command)
	{
		try
		{
			_logger.LogInformation("Creating new outlet: {OutletName}", command.OutletName);

			var commandWithUserId = command with { UserId = GetCurrentUserId() };
			var result = await _mediator.Send(commandWithUserId);

			if (!result.IsSuccess)
			{
				_logger.LogWarning("Failed to create outlet: {Error}", result.Error);
				return BadRequest(result.Error);
			}

			_logger.LogInformation("Successfully created outlet with Identifier: {OutletIdentifier}", result.Value?.OutletIdentifier);
			return CreatedAtAction(nameof(GetOutlet), new { outletIdentifier = result.Value?.OutletIdentifier }, result.Value);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error occurred while creating outlet");
			return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the outlet");
		}
	}

	private string GetCurrentUserId()
	{
		return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
				User.FindFirst("sub")?.Value ??
				User.FindFirst("oid")?.Value ??
				"system";
	}
}