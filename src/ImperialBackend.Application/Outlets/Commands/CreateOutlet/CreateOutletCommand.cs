using ImperialBackend.Application.Common.Models;
using ImperialBackend.Application.DTOs;
using ImperialBackend.Domain.Enums;
using MediatR;

namespace ImperialBackend.Application.Outlets.Commands.CreateOutlet;

/// <summary>
/// Command to create a new outlet
/// </summary>
public record CreateOutletCommand : IRequest<Result<OutletDto>>
{
    /// <summary>
    /// Gets the outlet name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the outlet tier
    /// </summary>
    public string Tier { get; init; } = string.Empty;

    /// <summary>
    /// Gets the outlet rank
    /// </summary>
    public int Rank { get; init; }

    /// <summary>
    /// Gets the chain type
    /// </summary>
    public ChainType ChainType { get; init; }

    /// <summary>
    /// Gets the initial sales amount
    /// </summary>
    public decimal Sales { get; init; }

    /// <summary>
    /// Gets the sales currency
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Gets the volume sold in kilograms
    /// </summary>
    public decimal VolumeSoldKg { get; init; }

    /// <summary>
    /// Gets the volume target in kilograms
    /// </summary>
    public decimal VolumeTargetKg { get; init; }

    /// <summary>
    /// Gets the outlet address
    /// </summary>
    public AddressDto Address { get; init; } = new();

    /// <summary>
    /// Gets the last visit date
    /// </summary>
    public DateTime? LastVisitDate { get; init; }

    /// <summary>
    /// Gets the stock risk
    /// </summary>
    public StockRisk StockRisk { get; init; } = StockRisk.Medium;

    /// <summary>
    /// Gets the user identifier who is creating the outlet
    /// </summary>
    public string UserId { get; init; } = string.Empty;
}