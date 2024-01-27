namespace EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.MonthlyQueries;

public class SpotPriceMonthlyQueryResult
{
    public string MeterPointId { get; set; }
    public int Month { get; set; }
    public string Production { get; set; } // Assuming Value is a string like "2.38 DKK/kWh"
}