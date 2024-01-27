using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.MonthlyQueries;

public class SpotPriceMonthlyQueryHandler(ICacheService cacheService, IProductionRepository productionRepository) : IRequestHandler<SpotPriceMonthlyQuery, List<SpotPriceMonthlyQueryResult>>
{
    public async Task<List<SpotPriceMonthlyQueryResult>> Handle(SpotPriceMonthlyQuery request, CancellationToken cancellationToken)
    {
        var productionResult = await productionRepository.SpotPriceMonthly(new PowerProductionFilter
        {
            StartDateTime = request.Start,
            EndDateTime = request.End,
        });

        var priceOfMonth = cacheService.RetrieveDateForMonth(request.Start, request.End);
        foreach (var data in productionResult)
        {
            if (priceOfMonth.TryGetValue(data.Month, out var multiplier))
            {
                data.Production *= multiplier; // Update the price
            }
        }

        var result = productionResult.Select(x => new SpotPriceMonthlyQueryResult
        {
            Month = x.Month,
            MeterPointId = x.MeterPointId,
            Production = $"{x.Production:F2} DKK/kWh"
        }).ToList();
        return result;
    }
}