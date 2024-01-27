namespace EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.MonthlyQueries;

public class SpotPriceMonthlyQuery : IRequest<List<SpotPriceMonthlyQueryResult>>
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}