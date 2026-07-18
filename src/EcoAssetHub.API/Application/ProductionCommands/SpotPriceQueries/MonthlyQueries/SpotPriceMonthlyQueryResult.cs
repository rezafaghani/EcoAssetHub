namespace EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.MonthlyQueries;

public class SpotPriceMonthlyQueryResult
{
    public required string MeterPointId { get; set; }
    public int Month { get; set; }
    public required string Production { get; set; }
}
