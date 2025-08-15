using ImperialBackend.Domain.Enums;

namespace ImperialBackend.Application.DTOs;

public class OutletDto
{
    public int Year { get; set; }
    public int Week { get; set; }
    public int TotalOuterQuantity { get; set; }
    public int CountOuterQuantity { get; set; }
    public decimal TotalSales6w { get; set; }
    public decimal Mean { get; set; }
    public decimal LowerLimit { get; set; }
    public decimal UpperLimit { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public int StoreRank { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string OutletIdentifier { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
}

public class CreateOutletDto
{
    public int Year { get; set; }
    public int Week { get; set; }
    public int TotalOuterQuantity { get; set; }
    public int CountOuterQuantity { get; set; }
    public decimal TotalSales6w { get; set; }
    public decimal Mean { get; set; }
    public decimal LowerLimit { get; set; }
    public decimal UpperLimit { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public int StoreRank { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string OutletIdentifier { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
}

public class UpdateOutletDto
{
    public int Year { get; set; }
    public int Week { get; set; }
    public int TotalOuterQuantity { get; set; }
    public int CountOuterQuantity { get; set; }
    public decimal TotalSales6w { get; set; }
    public decimal Mean { get; set; }
    public decimal LowerLimit { get; set; }
    public decimal UpperLimit { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public int StoreRank { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string OutletIdentifier { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string County { get; set; } = string.Empty;
}