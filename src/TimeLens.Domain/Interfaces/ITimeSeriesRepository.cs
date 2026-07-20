using TimeLens.Domain.Models;

namespace TimeLens.Domain.Interfaces;

public interface ITimeSeriesRepository
{
    Task<TimeSeriesInsertResult> InsertBatchAsync(TimeSeriesBatchRequest request, CancellationToken cancellationToken = default);
    Task<List<TimeSeriesPointDto>> GetSeriesAsync(string datasetId, DateTimeOffset start, DateTimeOffset end, DateTimeOffset? asOf, CancellationToken cancellationToken = default, int? limit = null);
}
