using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Infrastructure.Repositories;

public class TimeSeriesRepository(EcoAssetHubContext context) : ITimeSeriesRepository
{
    public async Task<TimeSeriesInsertResult> InsertBatchAsync(TimeSeriesBatchRequest request, CancellationToken cancellationToken = default)
    {
        var insertTime = DateTimeOffset.UtcNow;
        var result = new TimeSeriesInsertResult { AsOf = insertTime };

        await using var connection = context.CreateClickHouseConnection();
        await connection.OpenAsync(cancellationToken);

        foreach (var point in request.Points.OrderBy(x => x.Timestamp))
        {
            var latest = await GetLatestAsync(connection, request.DatasetId, point.Timestamp, cancellationToken);
            if (latest == point.Value)
            {
                result.Skipped++;
                continue;
            }

            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO energy_time_series_points (
                    dataset_id, timestamp, value, as_of, inserted_at, source_metadata_version)
                VALUES (
                    {datasetId:String}, {timestamp:DateTime64(3)}, {value:Nullable(Float64)},
                    {asOf:DateTime64(3)}, {insertedAt:DateTime64(3)}, {sourceMetadataVersion:String})
                """;
            command.AddParameter("datasetId", request.DatasetId);
            command.AddParameter("timestamp", DbValue.Utc(point.Timestamp));
            command.AddParameter("value", point.Value);
            command.AddParameter("asOf", DbValue.Utc(insertTime));
            command.AddParameter("insertedAt", DbValue.Utc(insertTime));
            command.AddParameter("sourceMetadataVersion", request.SourceMetadataVersion);
            await command.ExecuteNonQueryAsync(cancellationToken);

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
        await using var connection = context.CreateClickHouseConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $$"""
            SELECT timestamp, value, as_of
            FROM (
                SELECT
                    timestamp,
                    value,
                    as_of,
                    row_number() OVER (PARTITION BY dataset_id, timestamp ORDER BY as_of DESC, inserted_at DESC) AS rn
                FROM energy_time_series_points
                WHERE dataset_id = {datasetId:String}
                  AND timestamp >= {start:DateTime64(3)}
                  AND timestamp <= {end:DateTime64(3)}
                  {{(asOf.HasValue ? "AND as_of <= {asOf:DateTime64(3)}" : "")}}
            )
            WHERE rn = 1
            ORDER BY timestamp
            """;
        command.AddParameter("datasetId", datasetId);
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
                Value = reader.IsDBNull(1) ? null : reader.GetDouble(1),
                AsOf = new DateTimeOffset(reader.GetDateTime(2), TimeSpan.Zero)
            });
        }

        return result;
    }

    private static async Task<double?> GetLatestAsync(System.Data.Common.DbConnection connection, string datasetId, DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT value
            FROM energy_time_series_points
            WHERE dataset_id = {datasetId:String} AND timestamp = {timestamp:DateTime64(3)}
            ORDER BY as_of DESC, inserted_at DESC
            LIMIT 1
            """;
        command.AddParameter("datasetId", datasetId);
        command.AddParameter("timestamp", DbValue.Utc(timestamp));

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToDouble(value);
    }
}
