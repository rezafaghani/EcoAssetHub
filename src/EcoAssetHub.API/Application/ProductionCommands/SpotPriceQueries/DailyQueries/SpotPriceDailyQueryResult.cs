namespace EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.DailyQueries;

public class SpotPriceDailyQueryResult
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Value { get; set; } // Assuming Value is a string like "2.38 DKK/kWh" 
}