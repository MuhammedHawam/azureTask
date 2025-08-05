using ImperialBackend.Domain.Enums;

namespace ImperialBackend.Application.DTOs;

/// <summary>
/// Data transfer object for outlet information
/// </summary>
public class OutletDto
{
    /// <summary>
    /// Gets or sets the outlet identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the outlet name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outlet tier
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outlet rank
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the chain type
    /// </summary>
    public ChainType ChainType { get; set; }

    /// <summary>
    /// Gets or sets the last visit date
    /// </summary>
    public DateTime? LastVisitDate { get; set; }

    /// <summary>
    /// Gets or sets the sales amount
    /// </summary>
    public decimal Sales { get; set; }

    /// <summary>
    /// Gets or sets the sales currency
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the volume sold in kilograms
    /// </summary>
    public decimal VolumeSoldKg { get; set; }

    /// <summary>
    /// Gets or sets the volume target in kilograms
    /// </summary>
    public decimal VolumeTargetKg { get; set; }

    /// <summary>
    /// Gets or sets the outlet address
    /// </summary>
    public AddressDto Address { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the outlet is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last update date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who created the outlet
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the user who last updated the outlet
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets the target achievement percentage
    /// </summary>
    public decimal TargetAchievementPercentage => VolumeTargetKg == 0 ? 0 : Math.Round((VolumeSoldKg / VolumeTargetKg) * 100, 2);

    /// <summary>
    /// Gets a value indicating whether the outlet has achieved its target
    /// </summary>
    public bool HasAchievedTarget => VolumeSoldKg >= VolumeTargetKg && VolumeTargetKg > 0;

    /// <summary>
    /// Gets the number of days since last visit
    /// </summary>
    public int? DaysSinceLastVisit => LastVisitDate.HasValue ? (int)(DateTime.UtcNow.Date - LastVisitDate.Value.Date).TotalDays : null;

    /// <summary>
    /// Gets a value indicating whether the outlet needs a visit
    /// </summary>
    public bool NeedsVisit => DaysSinceLastVisit == null || DaysSinceLastVisit > 30;
}

/// <summary>
/// Data transfer object for address information
/// </summary>
public class AddressDto
{
    /// <summary>
    /// Gets or sets the street address
    /// </summary>
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the city
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the postal code
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the country
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets the full address
    /// </summary>
    public string FullAddress => $"{Street}, {City}, {State} {PostalCode}, {Country}";
}

/// <summary>
/// Data transfer object for creating a new outlet
/// </summary>
public class CreateOutletDto
{
    /// <summary>
    /// Gets or sets the outlet name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outlet tier
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outlet rank
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the chain type
    /// </summary>
    public ChainType ChainType { get; set; }

    /// <summary>
    /// Gets or sets the initial sales amount
    /// </summary>
    public decimal Sales { get; set; }

    /// <summary>
    /// Gets or sets the sales currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the volume sold in kilograms
    /// </summary>
    public decimal VolumeSoldKg { get; set; }

    /// <summary>
    /// Gets or sets the volume target in kilograms
    /// </summary>
    public decimal VolumeTargetKg { get; set; }

    /// <summary>
    /// Gets or sets the outlet address
    /// </summary>
    public AddressDto Address { get; set; } = new();

    /// <summary>
    /// Gets or sets the last visit date
    /// </summary>
    public DateTime? LastVisitDate { get; set; }
}

/// <summary>
/// Data transfer object for updating an outlet
/// </summary>
public class UpdateOutletDto
{
    /// <summary>
    /// Gets or sets the outlet name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outlet tier
    /// </summary>
    public string Tier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the outlet rank
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the chain type
    /// </summary>
    public ChainType ChainType { get; set; }

    /// <summary>
    /// Gets or sets the sales amount
    /// </summary>
    public decimal Sales { get; set; }

    /// <summary>
    /// Gets or sets the sales currency
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the volume sold in kilograms
    /// </summary>
    public decimal VolumeSoldKg { get; set; }

    /// <summary>
    /// Gets or sets the volume target in kilograms
    /// </summary>
    public decimal VolumeTargetKg { get; set; }

    /// <summary>
    /// Gets or sets the outlet address
    /// </summary>
    public AddressDto Address { get; set; } = new();

    /// <summary>
    /// Gets or sets the last visit date
    /// </summary>
    public DateTime? LastVisitDate { get; set; }
}