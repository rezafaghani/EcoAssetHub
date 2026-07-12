using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Infrastructure.Repositories;

public class TimeSeriesRepository(EcoAssetHubContext context) : ITimeSeriesRepository
{
    public async Task<TimeSeriesInsertResult> InsertBatchAsync(TimeSeriesBatchRequest request, CancellationToken cancellationToken = default)
    {
        var insertTime = DateTimeOffset.UtcNow;
        var result = new TimeSeriesInsertResult { AsOf = insertTime };

        foreach (var point in request.Points.OrderBy(x => x.Timestamp))
        {
            var existingVersions = await context.EnergyTimeSeriesPoints
                .Find(x => x.DatasetId == request.DatasetId && x.Timestamp == point.Timestamp)
                .ToListAsync(cancellationToken);

            var latest = existingVersions
                .OrderByDescending(x => x.AsOf)
                .ThenByDescending(x => x.InsertedAt)
                .FirstOrDefault();

            if (latest?.Value == point.Value)
            {
                result.Skipped++;
                continue;
            }

            await context.EnergyTimeSeriesPoints.InsertOneAsync(new EnergyTimeSeriesPoint
            {
                DatasetId = request.DatasetId,
                Timestamp = point.Timestamp,
                Value = point.Value,
                AsOf = insertTime,
                InsertedAt = insertTime,
                SourceMetadataVersion = request.SourceMetadataVersion
            }, cancellationToken: cancellationToken);

            result.Inserted++;
        }

        return result;
    }

    public async Task<List<TimeSeriesPointDto>> GetSeriesAsync(
        string datasetId,
        DateTimeOffset start,
        DateTimeOffset end,
        DateTimeOffset? asOf,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<EnergyTimeSeriesPoint>.Filter.Eq(x => x.DatasetId, datasetId) &
                     Builders<EnergyTimeSeriesPoint>.Filter.Gte(x => x.Timestamp, start) &
                     Builders<EnergyTimeSeriesPoint>.Filter.Lte(x => x.Timestamp, end);

        if (asOf.HasValue)
        {
            filter &= Builders<EnergyTimeSeriesPoint>.Filter.Lte(x => x.AsOf, asOf.Value);
        }

        var points = await context.EnergyTimeSeriesPoints.Find(filter).ToListAsync(cancellationToken);

        return points
            .GroupBy(x => new { x.DatasetId, x.Timestamp })
            .Select(group => group
                .OrderByDescending(x => x.AsOf)
                .ThenByDescending(x => x.InsertedAt)
                .First())
            .OrderBy(x => x.Timestamp)
            .Select(x => new TimeSeriesPointDto
            {
                Timestamp = x.Timestamp,
                Value = x.Value,
                AsOf = x.AsOf
            })
            .ToList();
    }
}
