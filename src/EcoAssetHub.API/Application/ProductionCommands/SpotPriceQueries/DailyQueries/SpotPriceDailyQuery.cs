namespace EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.DailyQueries;

public class SpotPriceDailyQuery : IRequest<List<SpotPriceDailyQueryResult>>
{
    public long MeterPointId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}