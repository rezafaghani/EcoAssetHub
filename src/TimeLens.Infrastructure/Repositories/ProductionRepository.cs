using TimeLens.Domain.Models;

namespace TimeLens.Infrastructure.Repositories;

public class ProductionRepository(TimeLensContext context) : IProductionRepository
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
        var points = await GetSeriesAsync(
            searchFilter.MeterPointId,
            searchFilter.StartDateTime,
            searchFilter.EndDateTime,
            null);

        return points
            .GroupBy(x => new { x.Timestamp.Year, x.Timestamp.Month })
            .Select(group => new PowerProductMonthlyDto
            {
                Month = group.Key.Month,
                Production = Convert.ToDecimal(group.Sum(x => x.Value ?? 0)),
                MeterPointId = searchFilter.MeterPointId.ToString()
            })
            .ToList();
    }

    public async Task<string> CreateAsync(PowerProduction input, CancellationToken cancellationToken = default)
    {
        var result = await InsertBatchAsync(new TimeSeriesBatchRequest
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
        }, cancellationToken);

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
        var meterPointId = request.MeterPointId.ToString();

        await using var connection = context.CreateClickHouseConnection();
        await connection.OpenAsync(cancellationToken);

        foreach (var point in request.Points.OrderBy(x => x.Timestamp))
        {
            var latest = await GetLatestProductionAsync(connection, meterPointId, point.Timestamp, cancellationToken);
            if (latest == point.Value)
            {
                result.Skipped++;
                continue;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO power_productions (
                    meter_point_id, production_datetime, production, as_of, inserted_at)
                VALUES (
                    {meterPointId:String}, {productionDateTime:DateTime64(3)}, {production:Int32},
                    {asOf:DateTime64(3)}, {insertedAt:DateTime64(3)})
                """;
            command.AddParameter("meterPointId", meterPointId);
            command.AddParameter("productionDateTime", DbValue.Utc(point.Timestamp));
            command.AddParameter("production", Convert.ToInt32(point.Value ?? 0));
            command.AddParameter("asOf", DbValue.Utc(insertTime));
            command.AddParameter("insertedAt", DbValue.Utc(insertTime));
            await command.ExecuteNonQueryAsync(cancellationToken);

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
        await using var connection = context.CreateClickHouseConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $$"""
            SELECT production_datetime, production, as_of
            FROM (
                SELECT
                    production_datetime,
                    production,
                    as_of,
                    row_number() OVER (PARTITION BY meter_point_id, production_datetime ORDER BY as_of DESC, inserted_at DESC) AS rn
                FROM power_productions
                WHERE meter_point_id = {meterPointId:String}
                  AND production_datetime >= {start:DateTime64(3)}
                  AND production_datetime <= {end:DateTime64(3)}
                  {{(asOf.HasValue ? "AND as_of <= {asOf:DateTime64(3)}" : "")}}
            )
            WHERE rn = 1
            ORDER BY production_datetime
            """;
        command.AddParameter("meterPointId", meterPointId.ToString());
        command.AddParameter("start", DbValue.Utc(start));
        command.AddParameter("end", DbValue.Utc(end));
        if (asOf.HasValue)
        {
            command.AddParameter("asOf", DbValue.Utc(asOf.Value));
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<TimeSeriesPointDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new TimeSeriesPointDto
            {
                Timestamp = new DateTimeOffset(reader.GetDateTime(0), TimeSpan.Zero),
                Value = reader.GetInt32(1),
                AsOf = new DateTimeOffset(reader.GetDateTime(2), TimeSpan.Zero)
            });
        }

        return result;
    }

    private static async Task<double?> GetLatestProductionAsync(System.Data.Common.DbConnection connection, string meterPointId, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT production
            FROM power_productions
            WHERE meter_point_id = {meterPointId:String} AND production_datetime = {timestamp:DateTime64(3)}
            ORDER BY as_of DESC, inserted_at DESC
            LIMIT 1
            """;
        command.AddParameter("meterPointId", meterPointId);
        command.AddParameter("timestamp", DbValue.Utc(timestamp));

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToDouble(value);
    }
}
