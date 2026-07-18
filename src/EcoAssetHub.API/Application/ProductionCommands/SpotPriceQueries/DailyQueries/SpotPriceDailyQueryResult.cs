namespace EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.DailyQueries;

public class SpotPriceDailyQueryResult
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public required string Value { get; set; }
}
