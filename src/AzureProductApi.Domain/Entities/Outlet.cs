using AzureProductApi.Domain.Common;
using AzureProductApi.Domain.Enums;
using AzureProductApi.Domain.ValueObjects;

namespace AzureProductApi.Domain.Entities;

/// <summary>
/// Represents an outlet entity with business rules and invariants
/// </summary>
public class Outlet : BaseEntity
{
    /// <summary>
    /// Initializes a new instance of the Outlet class
    /// </summary>
    /// <param name="name">The outlet name</param>
    /// <param name="tier">The outlet tier</param>
    /// <param name="rank">The outlet rank</param>
    /// <param name="chainType">The chain type (Regional or National)</param>
    /// <param name="address">The outlet address</param>
    public Outlet(string name, string tier, int rank, ChainType chainType, Address address)
    {
        Name = ValidateName(name);
        Tier = ValidateTier(tier);
        Rank = ValidateRank(rank);
        ChainType = chainType;
        Address = address ?? throw new ArgumentNullException(nameof(address));
        LastVisitDate = null;
        Sales = Money.Usd(0);
        VolumeSoldKg = 0;
        VolumeTargetKg = 0;
        IsActive = true;
    }

    // Parameterless constructor for EF Core
    private Outlet() { }

    /// <summary>
    /// Gets the outlet name
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the outlet tier
    /// </summary>
    public string Tier { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the outlet rank
    /// </summary>
    public int Rank { get; private set; }

    /// <summary>
    /// Gets the chain type (Regional or National)
    /// </summary>
    public ChainType ChainType { get; private set; }

    /// <summary>
    /// Gets the last visit date
    /// </summary>
    public DateTime? LastVisitDate { get; private set; }

    /// <summary>
    /// Gets the sales amount
    /// </summary>
    public Money Sales { get; private set; } = null!;

    /// <summary>
    /// Gets the volume sold in kilograms
    /// </summary>
    public decimal VolumeSoldKg { get; private set; }

    /// <summary>
    /// Gets the volume target in kilograms
    /// </summary>
    public decimal VolumeTargetKg { get; private set; }

    /// <summary>
    /// Gets the outlet address
    /// </summary>
    public Address Address { get; private set; } = null!;

    /// <summary>
    /// Gets a value indicating whether the outlet is active
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Updates the outlet information
    /// </summary>
    /// <param name="name">The new outlet name</param>
    /// <param name="tier">The new outlet tier</param>
    /// <param name="rank">The new outlet rank</param>
    /// <param name="chainType">The new chain type</param>
    /// <param name="address">The new outlet address</param>
    /// <param name="userId">The user making the update</param>
    public void UpdateOutlet(string name, string tier, int rank, ChainType chainType, Address address, string userId)
    {
        Name = ValidateName(name);
        Tier = ValidateTier(tier);
        Rank = ValidateRank(rank);
        ChainType = chainType;
        Address = address ?? throw new ArgumentNullException(nameof(address));
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Records a visit to the outlet
    /// </summary>
    /// <param name="visitDate">The visit date</param>
    /// <param name="userId">The user recording the visit</param>
    public void RecordVisit(DateTime visitDate, string userId)
    {
        if (visitDate > DateTime.UtcNow)
            throw new ArgumentException("Visit date cannot be in the future", nameof(visitDate));

        LastVisitDate = visitDate;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Updates the sales amount
    /// </summary>
    /// <param name="sales">The new sales amount</param>
    /// <param name="userId">The user making the update</param>
    public void UpdateSales(Money sales, string userId)
    {
        Sales = sales ?? throw new ArgumentNullException(nameof(sales));
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Updates the volume sold
    /// </summary>
    /// <param name="volumeSoldKg">The new volume sold in kilograms</param>
    /// <param name="userId">The user making the update</param>
    public void UpdateVolumeSold(decimal volumeSoldKg, string userId)
    {
        if (volumeSoldKg < 0)
            throw new ArgumentException("Volume sold cannot be negative", nameof(volumeSoldKg));

        VolumeSoldKg = volumeSoldKg;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Updates the volume target
    /// </summary>
    /// <param name="volumeTargetKg">The new volume target in kilograms</param>
    /// <param name="userId">The user making the update</param>
    public void UpdateVolumeTarget(decimal volumeTargetKg, string userId)
    {
        if (volumeTargetKg < 0)
            throw new ArgumentException("Volume target cannot be negative", nameof(volumeTargetKg));

        VolumeTargetKg = volumeTargetKg;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Activates the outlet
    /// </summary>
    /// <param name="userId">The user making the update</param>
    public void Activate(string userId)
    {
        IsActive = true;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Deactivates the outlet
    /// </summary>
    /// <param name="userId">The user making the update</param>
    public void Deactivate(string userId)
    {
        IsActive = false;
        UpdateAuditInfo(userId);
    }

    /// <summary>
    /// Gets the target achievement percentage
    /// </summary>
    /// <returns>The percentage of target achieved</returns>
    public decimal GetTargetAchievementPercentage()
    {
        if (VolumeTargetKg == 0)
            return 0;

        return Math.Round((VolumeSoldKg / VolumeTargetKg) * 100, 2);
    }

    /// <summary>
    /// Checks if the outlet has achieved its target
    /// </summary>
    /// <returns>True if target is achieved, false otherwise</returns>
    public bool HasAchievedTarget() => VolumeSoldKg >= VolumeTargetKg && VolumeTargetKg > 0;

    /// <summary>
    /// Gets the number of days since last visit
    /// </summary>
    /// <returns>The number of days since last visit, null if never visited</returns>
    public int? GetDaysSinceLastVisit()
    {
        if (!LastVisitDate.HasValue)
            return null;

        return (int)(DateTime.UtcNow.Date - LastVisitDate.Value.Date).TotalDays;
    }

    /// <summary>
    /// Checks if the outlet needs a visit (based on last visit being more than specified days ago)
    /// </summary>
    /// <param name="maxDaysSinceVisit">Maximum allowed days since last visit</param>
    /// <returns>True if outlet needs a visit, false otherwise</returns>
    public bool NeedsVisit(int maxDaysSinceVisit = 30)
    {
        var daysSinceLastVisit = GetDaysSinceLastVisit();
        return !daysSinceLastVisit.HasValue || daysSinceLastVisit.Value > maxDaysSinceVisit;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Outlet name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Outlet name cannot exceed 200 characters", nameof(name));

        return name.Trim();
    }

    private static string ValidateTier(string tier)
    {
        if (string.IsNullOrWhiteSpace(tier))
            throw new ArgumentException("Outlet tier cannot be empty", nameof(tier));

        if (tier.Length > 50)
            throw new ArgumentException("Outlet tier cannot exceed 50 characters", nameof(tier));

        return tier.Trim();
    }

    private static int ValidateRank(int rank)
    {
        if (rank <= 0)
            throw new ArgumentException("Outlet rank must be greater than zero", nameof(rank));

        return rank;
    }
}