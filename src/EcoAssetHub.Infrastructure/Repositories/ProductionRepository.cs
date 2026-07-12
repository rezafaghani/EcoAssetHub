using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Infrastructure.Repositories;

public class ProductionRepository(EcoAssetHubContext context) : IProductionRepository
{
    public async Task<List<PowerProductPerDayDto>> SpotPricesDaily(PowerProductionFilter searchFilter)
    {
        var points = await GetSeriesAsync(
            searchFilter.MeterPointId,
            searchFilter.StartDateTime,
            searchFilter.EndDateTime,
            null);

        return points
            .GroupBy(x => x.Timestamp.Date)
            .Select(group => new PowerProductPerDayDto
            {
                Start = group.Min(x => x.Timestamp.DateTime),
                End = group.Max(x => x.Timestamp.DateTime),
                Production = Convert.ToDecimal(group.Sum(x => x.Value ?? 0))
            })
            .ToList();
    }

    public async Task<List<PowerProductMonthlyDto>> SpotPriceMonthly(PowerProductionFilter searchFilter)
    {
        var filter = Builders<PowerProduction>.Filter.Gte(x => x.ProductionDateTime, searchFilter.StartDateTime) &
                     Builders<PowerProduction>.Filter.Lte(x => x.ProductionDateTime, searchFilter.EndDateTime);

        var productions = await context.PowerProductions.Find(filter).ToListAsync();
        var latestVersions = ResolveLatestVersions(productions, null);

        return latestVersions
            .GroupBy(x => new { x.ProductionDateTime.Year, x.ProductionDateTime.Month, x.MeterPointId })
            .Select(group => new PowerProductMonthlyDto
            {
                Month = group.Key.Month,
                Production = group.Sum(x => x.Production),
                MeterPointId = group.Key.MeterPointId
            })
            .ToList();
    }

    public async Task<string> CreateAsync(PowerProduction input, CancellationToken cancellationToken = default)
    {
        var request = new TimeSeriesBatchRequest
        {
            MeterPointId = long.Parse(input.MeterPointId),
            Points =
            [
                new TimeSeriesWritePoint
                {
                    Timestamp = input.ProductionDateTime,
                    Value = input.Production
                }
            ]
        };

        var result = await InsertBatchAsync(request, cancellationToken);
        return result.Inserted == 1 ? input.Id : string.Empty;
    }

    public async Task CreateListAsync(List<PowerProduction> input, CancellationToken cancellationToken = default)
    {
        foreach (var group in input.GroupBy(x => x.MeterPointId))
        {
            if (!long.TryParse(group.Key, out var meterPointId))
            {
                continue;
            }

            await InsertBatchAsync(new TimeSeriesBatchRequest
            {
                MeterPointId = meterPointId,
                Points = group.Select(x => new TimeSeriesWritePoint
                {
                    Timestamp = x.ProductionDateTime,
                    Value = x.Production
                }).ToList()
            }, cancellationToken);
        }
    }

    public async Task<TimeSeriesInsertResult> InsertBatchAsync(TimeSeriesBatchRequest request, CancellationToken cancellationToken = default)
    {
        var insertTime = DateTimeOffset.UtcNow;
        var result = new TimeSeriesInsertResult { AsOf = insertTime };

        foreach (var point in request.Points.OrderBy(x => x.Timestamp))
        {
            var meterPointId = request.MeterPointId.ToString();
            var existingVersions = await context.PowerProductions
                .Find(x => x.MeterPointId == meterPointId && x.ProductionDateTime == point.Timestamp)
                .ToListAsync(cancellationToken);

            var latest = existingVersions
                .OrderByDescending(x => x.AsOf)
                .ThenByDescending(x => x.InsertedAt)
                .FirstOrDefault();

            if (latest?.Production == point.Value)
            {
                result.Skipped++;
                continue;
            }

            await context.PowerProductions.InsertOneAsync(new PowerProduction
            {
                MeterPointId = meterPointId,
                ProductionDateTime = point.Timestamp,
                Production = Convert.ToInt32(point.Value ?? 0),
                AsOf = insertTime,
                InsertedAt = insertTime
            }, cancellationToken: cancellationToken);

            result.Inserted++;
        }

        return result;
    }

    public async Task<List<TimeSeriesPointDto>> GetSeriesAsync(
        long meterPointId,
        DateTimeOffset start,
        DateTimeOffset end,
        DateTimeOffset? asOf,
        CancellationToken cancellationToken = default)
    {
        var meterPoint = meterPointId.ToString();
        var filter = Builders<PowerProduction>.Filter.Eq(x => x.MeterPointId, meterPoint) &
                     Builders<PowerProduction>.Filter.Gte(x => x.ProductionDateTime, start) &
                     Builders<PowerProduction>.Filter.Lte(x => x.ProductionDateTime, end);

        if (asOf.HasValue)
        {
            filter &= Builders<PowerProduction>.Filter.Lte(x => x.AsOf, asOf.Value);
        }

        var productions = await context.PowerProductions.Find(filter).ToListAsync(cancellationToken);

        return ResolveLatestVersions(productions, asOf)
            .OrderBy(x => x.ProductionDateTime)
            .Select(x => new TimeSeriesPointDto
            {
                Timestamp = x.ProductionDateTime,
                Value = x.Production,
                AsOf = x.AsOf
            })
            .ToList();
    }

    private static List<PowerProduction> ResolveLatestVersions(List<PowerProduction> productions, DateTimeOffset? asOf)
    {
        var filtered = asOf.HasValue
            ? productions.Where(x => x.AsOf <= asOf.Value)
            : productions;

        return filtered
            .GroupBy(x => new { x.MeterPointId, x.ProductionDateTime })
            .Select(group => group
                .OrderByDescending(x => x.AsOf)
                .ThenByDescending(x => x.InsertedAt)
                .First())
            .ToList();
    }
}
