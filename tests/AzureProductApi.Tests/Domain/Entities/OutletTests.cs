using AzureProductApi.Domain.Entities;
using AzureProductApi.Domain.Enums;
using AzureProductApi.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AzureProductApi.Tests.Domain.Entities;

public class OutletTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateOutlet()
    {
        // Arrange
        var name = "Test Outlet";
        var tier = "Premium";
        var rank = 1;
        var chainType = ChainType.National;
        var address = new Address("123 Main St", "New York", "NY", "10001", "USA");

        // Act
        var outlet = new Outlet(name, tier, rank, chainType, address);

        // Assert
        outlet.Name.Should().Be(name);
        outlet.Tier.Should().Be(tier);
        outlet.Rank.Should().Be(rank);
        outlet.ChainType.Should().Be(chainType);
        outlet.Address.Should().Be(address);
        outlet.IsActive.Should().BeTrue();
        outlet.VolumeSoldKg.Should().Be(0);
        outlet.VolumeTargetKg.Should().Be(0);
        outlet.Sales.Amount.Should().Be(0);
        outlet.Sales.Currency.Should().Be("USD");
        outlet.LastVisitDate.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Arrange
        var tier = "Premium";
        var rank = 1;
        var chainType = ChainType.National;
        var address = new Address("123 Main St", "New York", "NY", "10001", "USA");

        // Act & Assert
        var action = () => new Outlet(invalidName, tier, rank, chainType, address);
        action.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var name = new string('A', 201); // Too long
        var tier = "Premium";
        var rank = 1;
        var chainType = ChainType.National;
        var address = new Address("123 Main St", "New York", "NY", "10001", "USA");

        // Act & Assert
        var action = () => new Outlet(name, tier, rank, chainType, address);
        action.Should().Throw<ArgumentException>().WithMessage("*200 characters*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidRank_ShouldThrowArgumentException(int invalidRank)
    {
        // Arrange
        var name = "Test Outlet";
        var tier = "Premium";
        var chainType = ChainType.National;
        var address = new Address("123 Main St", "New York", "NY", "10001", "USA");

        // Act & Assert
        var action = () => new Outlet(name, tier, invalidRank, chainType, address);
        action.Should().Throw<ArgumentException>().WithMessage("*rank*");
    }

    [Fact]
    public void UpdateOutlet_WithValidParameters_ShouldUpdateProperties()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var newName = "Updated Outlet";
        var newTier = "Standard";
        var newRank = 5;
        var newChainType = ChainType.Regional;
        var newAddress = new Address("456 Oak Ave", "Boston", "MA", "02101", "USA");
        var userId = "user123";

        // Act
        outlet.UpdateOutlet(newName, newTier, newRank, newChainType, newAddress, userId);

        // Assert
        outlet.Name.Should().Be(newName);
        outlet.Tier.Should().Be(newTier);
        outlet.Rank.Should().Be(newRank);
        outlet.ChainType.Should().Be(newChainType);
        outlet.Address.Should().Be(newAddress);
        outlet.UpdatedBy.Should().Be(userId);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordVisit_WithValidDate_ShouldUpdateLastVisitDate()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var visitDate = DateTime.UtcNow.AddDays(-1);
        var userId = "user123";

        // Act
        outlet.RecordVisit(visitDate, userId);

        // Assert
        outlet.LastVisitDate.Should().Be(visitDate);
        outlet.UpdatedBy.Should().Be(userId);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordVisit_WithFutureDate_ShouldThrowArgumentException()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var futureDate = DateTime.UtcNow.AddDays(1);
        var userId = "user123";

        // Act & Assert
        var action = () => outlet.RecordVisit(futureDate, userId);
        action.Should().Throw<ArgumentException>().WithMessage("*future*");
    }

    [Fact]
    public void UpdateSales_WithValidAmount_ShouldUpdateSales()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var newSales = Money.Usd(15000.50m);
        var userId = "user123";

        // Act
        outlet.UpdateSales(newSales, userId);

        // Assert
        outlet.Sales.Should().Be(newSales);
        outlet.UpdatedBy.Should().Be(userId);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateVolumeSold_WithValidAmount_ShouldUpdateVolume()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var newVolume = 1500.75m;
        var userId = "user123";

        // Act
        outlet.UpdateVolumeSold(newVolume, userId);

        // Assert
        outlet.VolumeSoldKg.Should().Be(newVolume);
        outlet.UpdatedBy.Should().Be(userId);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateVolumeSold_WithNegativeAmount_ShouldThrowArgumentException()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var negativeVolume = -100m;
        var userId = "user123";

        // Act & Assert
        var action = () => outlet.UpdateVolumeSold(negativeVolume, userId);
        action.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void UpdateVolumeTarget_WithValidAmount_ShouldUpdateTarget()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var newTarget = 2000m;
        var userId = "user123";

        // Act
        outlet.UpdateVolumeTarget(newTarget, userId);

        // Assert
        outlet.VolumeTargetKg.Should().Be(newTarget);
        outlet.UpdatedBy.Should().Be(userId);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        outlet.Deactivate("user123"); // First deactivate
        var userId = "user456";

        // Act
        outlet.Activate(userId);

        // Assert
        outlet.IsActive.Should().BeTrue();
        outlet.UpdatedBy.Should().Be(userId);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var userId = "user123";

        // Act
        outlet.Deactivate(userId);

        // Assert
        outlet.IsActive.Should().BeFalse();
        outlet.UpdatedBy.Should().Be(userId);
        outlet.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(1000, 1000, 100)]
    [InlineData(1500, 1000, 150)]
    [InlineData(500, 1000, 50)]
    [InlineData(0, 1000, 0)]
    [InlineData(1000, 0, 0)]
    public void GetTargetAchievementPercentage_ShouldCalculateCorrectly(decimal sold, decimal target, decimal expected)
    {
        // Arrange
        var outlet = CreateValidOutlet();
        outlet.UpdateVolumeSold(sold, "user123");
        outlet.UpdateVolumeTarget(target, "user123");

        // Act
        var result = outlet.GetTargetAchievementPercentage();

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(1000, 1000, true)]
    [InlineData(1500, 1000, true)]
    [InlineData(500, 1000, false)]
    [InlineData(0, 1000, false)]
    [InlineData(1000, 0, false)]
    public void HasAchievedTarget_ShouldReturnCorrectResult(decimal sold, decimal target, bool expected)
    {
        // Arrange
        var outlet = CreateValidOutlet();
        outlet.UpdateVolumeSold(sold, "user123");
        outlet.UpdateVolumeTarget(target, "user123");

        // Act
        var result = outlet.HasAchievedTarget();

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetDaysSinceLastVisit_WithNoVisit_ShouldReturnNull()
    {
        // Arrange
        var outlet = CreateValidOutlet();

        // Act
        var result = outlet.GetDaysSinceLastVisit();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDaysSinceLastVisit_WithVisit_ShouldReturnCorrectDays()
    {
        // Arrange
        var outlet = CreateValidOutlet();
        var visitDate = DateTime.UtcNow.AddDays(-5).Date;
        outlet.RecordVisit(visitDate, "user123");

        // Act
        var result = outlet.GetDaysSinceLastVisit();

        // Assert
        result.Should().Be(5);
    }

    [Theory]
    [InlineData(null, 30, true)] // Never visited
    [InlineData(-35, 30, true)]  // Visited 35 days ago, max 30
    [InlineData(-25, 30, false)] // Visited 25 days ago, max 30
    [InlineData(-30, 30, false)] // Visited exactly 30 days ago
    public void NeedsVisit_ShouldReturnCorrectResult(int? daysAgo, int maxDays, bool expected)
    {
        // Arrange
        var outlet = CreateValidOutlet();
        if (daysAgo.HasValue)
        {
            var visitDate = DateTime.UtcNow.AddDays(daysAgo.Value);
            outlet.RecordVisit(visitDate, "user123");
        }

        // Act
        var result = outlet.NeedsVisit(maxDays);

        // Assert
        result.Should().Be(expected);
    }

    private static Outlet CreateValidOutlet()
    {
        var address = new Address("123 Main St", "New York", "NY", "10001", "USA");
        return new Outlet("Test Outlet", "Premium", 1, ChainType.National, address);
    }
}