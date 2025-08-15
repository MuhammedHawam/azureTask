using ImperialBackend.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace ImperialBackend.Tests.Domain.Entities;

public class OutletTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateOutlet()
    {
        // Arrange
        var year = 2019;
        var week = 23;
        var totalOuterQuantity = 1;
        var countOuterQuantity = 1;
        var totalSales6w = 3m;
        var mean = 14.087557603686635m;
        var lowerLimit = 4m;
        var upperLimit = 18m;
        var healthStatus = "red";
        var storeRank = 173;
        var outletName = "OMEGNA 0002";
        var outletIdentifier = "001w000001ZUPO8AAP";
        var addressLine1 = "PIAZZA BELTRAMI 21";
        var state = "VB";
        var county = "PIEMONTE";

        // Act
        var outlet = new Outlet(year, week, totalOuterQuantity, countOuterQuantity, totalSales6w, mean, lowerLimit, upperLimit,
            healthStatus, storeRank, outletName, outletIdentifier, addressLine1, state, county);

        // Assert
        outlet.Year.Should().Be(year);
        outlet.Week.Should().Be(week);
        outlet.TotalOuterQuantity.Should().Be(totalOuterQuantity);
        outlet.CountOuterQuantity.Should().Be(countOuterQuantity);
        outlet.TotalSales6w.Should().Be(totalSales6w);
        outlet.Mean.Should().Be(mean);
        outlet.LowerLimit.Should().Be(lowerLimit);
        outlet.UpperLimit.Should().Be(upperLimit);
        outlet.HealthStatus.Should().Be(healthStatus);
        outlet.StoreRank.Should().Be(storeRank);
        outlet.OutletName.Should().Be(outletName);
        outlet.OutletIdentifier.Should().Be(outletIdentifier);
        outlet.AddressLine1.Should().Be(addressLine1);
        outlet.State.Should().Be(state);
        outlet.County.Should().Be(county);
    }
}