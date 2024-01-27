using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.DailyQueries;

public class SpotPriceDailyQueryHandler(ICacheService cacheService, IProductionRepository productionRepository)
    : IRequestHandler<SpotPriceDailyQuery, List<SpotPriceDailyQueryResult>>
{
    public async Task<List<SpotPriceDailyQueryResult>> Handle(SpotPriceDailyQuery request, CancellationToken cancellationToken)
    {
        var productionResult = await productionRepository.SpotPricesDaily(new PowerProductionFilter
        {
            StartDateTime = request.Start,
            EndDateTime = request.End,
            MeterPointId = request.MeterPointId
        });
        foreach (var item in productionResult)
        {
            var priceOfDay = cacheService.RetrieveByDateTime(item.Start.Date);
            if (priceOfDay != null)
                item.Production *= priceOfDay.Price;
        }

        var result = productionResult.Select(x => new SpotPriceDailyQueryResult
        {
            Start = x.Start,
            End = x.End,
            Value = $"{x.Production:F2} DKK/kWh"
        }).ToList();
        return result;
    }
}