using AutoMapper;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Application.Outlets.Commands.CreateOutlet;

/// <summary>
/// Handler for CreateOutletCommand
/// </summary>
public class CreateOutletCommandHandler : IRequestHandler<CreateOutletCommand, Result<OutletDto>>
{
    private readonly IOutletRepository _outletRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOutletCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the CreateOutletCommandHandler class
    /// </summary>
    /// <param name="outletRepository">The outlet repository</param>
    /// <param name="mapper">The AutoMapper instance</param>
    /// <param name="logger">The logger</param>
    public CreateOutletCommandHandler(
        IOutletRepository outletRepository,
        IMapper mapper,
        ILogger<CreateOutletCommandHandler> logger)
    {
        _outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the CreateOutletCommand
    /// </summary>
    /// <param name="request">The command request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A result containing the created outlet or error information</returns>
    public async Task<Result<OutletDto>> Handle(CreateOutletCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating outlet: {Name} in {City}, {State}", 
                request.Name, request.Address.City, request.Address.State);

            // Note: Duplicate validation removed for performance - can be added back if needed

            // Create the Address value object
            var address = new Address(
                request.Address.Street,
                request.Address.City,
                request.Address.State,
                request.Address.PostalCode,
                request.Address.Country);

            // Create the outlet entity
            var outlet = new Outlet(
                request.Name,
                request.Tier,
                request.Rank,
                request.ChainType,
                address);

            // Set additional properties
            var sales = new Money(request.Sales, request.Currency);
            outlet.UpdateSales(sales, request.UserId);
            outlet.UpdateVolumeSold(request.VolumeSoldKg, request.UserId);
            outlet.UpdateVolumeTarget(request.VolumeTargetKg, request.UserId);
            outlet.SetCreationInfo(request.UserId);

            // Record visit if provided
            if (request.LastVisitDate.HasValue)
            {
                outlet.RecordVisit(request.LastVisitDate.Value, request.UserId);
            }

            // Save to repository
            var createdOutlet = await _outletRepository.AddAsync(outlet, cancellationToken);

            // Map to DTO
            var outletDto = _mapper.Map<OutletDto>(createdOutlet);

            _logger.LogInformation("Successfully created outlet with ID: {OutletId}", createdOutlet.Id);

            return Result<OutletDto>.Success(outletDto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating outlet: {Message}", ex.Message);
            return Result<OutletDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating outlet: {Name}", request.Name);
            return Result<OutletDto>.Failure("An error occurred while creating the outlet");
        }
    }
}