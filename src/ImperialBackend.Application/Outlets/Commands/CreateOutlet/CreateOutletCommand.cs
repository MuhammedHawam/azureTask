using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using MediatR;

namespace ImperialBackend.Application.Outlets.Commands.CreateOutlet;

public record CreateOutletCommand : IRequest<Result<OutletDto>>
{
    public int Year { get; init; }
    public int Week { get; init; }
    public int TotalOuterQuantity { get; init; }
    public int CountOuterQuantity { get; init; }
    public decimal TotalSales6w { get; init; }
    public decimal Mean { get; init; }
    public decimal LowerLimit { get; init; }
    public decimal UpperLimit { get; init; }
    public string HealthStatus { get; init; } = string.Empty;
    public int StoreRank { get; init; }
    public string OutletName { get; init; } = string.Empty;
    public string OutletIdentifier { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string County { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}