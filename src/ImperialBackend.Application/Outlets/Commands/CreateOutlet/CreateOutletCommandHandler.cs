using AutoMapper;
using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Domain.Entities;
using ImperialBackend.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ImperialBackend.Application.Outlets.Commands.CreateOutlet;

public class CreateOutletCommandHandler : IRequestHandler<CreateOutletCommand, Result<OutletDto>>
{
    private readonly IOutletRepository _outletRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateOutletCommandHandler> _logger;

    public CreateOutletCommandHandler(
        IOutletRepository outletRepository,
        IMapper mapper,
        ILogger<CreateOutletCommandHandler> logger)
    {
        _outletRepository = outletRepository ?? throw new ArgumentNullException(nameof(outletRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<OutletDto>> Handle(CreateOutletCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating outlet: {OutletName} ({OutletIdentifier})", request.OutletName, request.OutletIdentifier);

            var outlet = new Outlet(
                request.Year,
                request.Week,
                request.TotalOuterQuantity,
                request.CountOuterQuantity,
                request.TotalSales6w,
                request.Mean,
                request.LowerLimit,
                request.UpperLimit,
                request.HealthStatus,
                request.StoreRank,
                request.OutletName,
                request.OutletIdentifier,
                request.AddressLine1,
                request.State,
                request.County);

            outlet.SetCreationInfo(request.UserId);

            var createdOutlet = await _outletRepository.AddAsync(outlet, cancellationToken);
            var dto = _mapper.Map<OutletDto>(createdOutlet);
            return Result<OutletDto>.Success(dto);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating outlet: {Message}", ex.Message);
            return Result<OutletDto>.Failure(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating outlet: {OutletIdentifier}", request.OutletIdentifier);
            return Result<OutletDto>.Failure("An error occurred while creating the outlet");
        }
    }
}