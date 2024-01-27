using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Domain.Interfaces;

public interface IProductionRepository
{
    Task<List<PowerProductPerDayDto>> SpotPricesDaily(PowerProductionFilter filter);
    Task<List<PowerProductMonthlyDto>> SpotPriceMonthly(PowerProductionFilter filter);
    Task<string> CreateAsync(PowerProduction input, CancellationToken cancellationToken = default);
    Task CreateListAsync(List<PowerProduction> input, CancellationToken cancellationToken = default);
}