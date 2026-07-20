using TimeLens.Domain.Models;

namespace TimeLens.Domain.Interfaces;

public interface IProductionRepository
{
    Task<List<PowerProductPerDayDto>> SpotPricesDaily(PowerProductionFilter filter);
    Task<List<PowerProductMonthlyDto>> SpotPriceMonthly(PowerProductionFilter filter);
    Task<string> CreateAsync(PowerProduction input, CancellationToken cancellationToken = default);
    Task CreateListAsync(List<PowerProduction> input, CancellationToken cancellationToken = default);
    Task<TimeSeriesInsertResult> InsertBatchAsync(TimeSeriesBatchRequest request, CancellationToken cancellationToken = default);
    Task<List<TimeSeriesPointDto>> GetSeriesAsync(long meterPointId, DateTimeOffset start, DateTimeOffset end, DateTimeOffset? asOf, CancellationToken cancellationToken = default);
}
