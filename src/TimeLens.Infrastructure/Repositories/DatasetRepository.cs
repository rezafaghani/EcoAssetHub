using System.Text.Json;
using TimeLens.Domain.Models;
using Npgsql;
using NpgsqlTypes;

namespace TimeLens.Infrastructure.Repositories;

public class DatasetRepository(TimeLensContext context) : IDatasetRepository
{
    public async Task<DatasetMetadataDto> UpsertAsync(DatasetMetadataDto metadata, CancellationToken cancellationToken = default)
    {
        metadata.Id = string.IsNullOrWhiteSpace(metadata.Id) ? CreateDatasetId(metadata) : metadata.Id;

        var now = DateTimeOffset.UtcNow;
        if (metadata.FirstObservedAt == default)
        {
            metadata.FirstObservedAt = now;
        }

        metadata.LastIngestedAt = now;

        await using var command = context.Postgres.CreateCommand("""
            INSERT INTO energy_datasets (
                id, curve_id, source, endpoint, metric, data_kind, category, unit, country, bidding_zone, region,
                granularity, production_type, forecast_type, neighbor, license_info, deprecated,
                request_parameters, first_observed_at, last_ingested_at)
            VALUES (
                @id, @curve_id, @source, @endpoint, @metric, @data_kind, @category, @unit, @country, @bidding_zone, @region,
                @granularity, @production_type, @forecast_type, @neighbor, @license_info, @deprecated,
                @request_parameters, @first_observed_at, @last_ingested_at)
            ON CONFLICT (id) DO UPDATE SET
                curve_id = EXCLUDED.curve_id,
                source = EXCLUDED.source,
                endpoint = EXCLUDED.endpoint,
                metric = EXCLUDED.metric,
                data_kind = EXCLUDED.data_kind,
                category = EXCLUDED.category,
                unit = EXCLUDED.unit,
                country = EXCLUDED.country,
                bidding_zone = EXCLUDED.bidding_zone,
                region = EXCLUDED.region,
                granularity = EXCLUDED.granularity,
                production_type = EXCLUDED.production_type,
                forecast_type = EXCLUDED.forecast_type,
                neighbor = EXCLUDED.neighbor,
                license_info = EXCLUDED.license_info,
                deprecated = EXCLUDED.deprecated,
                request_parameters = EXCLUDED.request_parameters,
                last_ingested_at = EXCLUDED.last_ingested_at
            """);
        AddParameters(command, metadata);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return metadata;
    }

    public async Task<DatasetMetadataDto?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var command = context.Postgres.CreateCommand($"""
            {SelectSql}
            WHERE id = @id
            LIMIT 1
            """);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ToDto(reader) : null;
    }

    public async Task<List<DatasetMetadataDto>> SearchAsync(DatasetSearchFilter filter, CancellationToken cancellationToken = default)
    {
        await using var command = context.Postgres.CreateCommand();
        var clauses = new List<string>();

        AddFilter(command, clauses, "endpoint", filter.Endpoint);
        AddFilter(command, clauses, "curve_id", filter.CurveId);
        AddFilter(command, clauses, "metric", filter.Metric);
        AddFilter(command, clauses, "data_kind", filter.DataKind);
        AddFilter(command, clauses, "category", filter.Category);
        AddFilter(command, clauses, "country", filter.Country);
        AddFilter(command, clauses, "bidding_zone", filter.BiddingZone);
        AddFilter(command, clauses, "region", filter.Region);
        AddFilter(command, clauses, "granularity", filter.Granularity);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            clauses.Add("""
                (
                    id ILIKE @search OR
                    curve_id ILIKE @search OR
                    endpoint ILIKE @search OR
                    metric ILIKE @search OR
                    data_kind ILIKE @search OR
                    category ILIKE @search OR
                    unit ILIKE @search OR
                    country ILIKE @search OR
                    bidding_zone ILIKE @search OR
                    region ILIKE @search OR
                    production_type ILIKE @search OR
                    forecast_type ILIKE @search OR
                    neighbor ILIKE @search
                )
                """);
            command.Parameters.AddWithValue("search", $"%{filter.Search.Trim()}%");
        }

        command.CommandText = $"""
            {SelectSql}
            {(clauses.Count == 0 ? "" : $"WHERE {string.Join(" AND ", clauses)}")}
            ORDER BY last_ingested_at DESC
            LIMIT 500
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<DatasetMetadataDto>();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ToDto(reader));
        }

        return result;
    }

    public async Task<DatasetMetadataDto?> SetDeprecatedAsync(string id, bool deprecated, CancellationToken cancellationToken = default)
    {
        await using var command = context.Postgres.CreateCommand("""
            UPDATE energy_datasets
            SET deprecated = @deprecated
            WHERE id = @id
            """);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("deprecated", deprecated);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return await GetAsync(id, cancellationToken);
    }

    private const string SelectSql = """
        SELECT id, curve_id, source, endpoint, metric, data_kind, category, unit, country, bidding_zone, region,
               granularity, production_type, forecast_type, neighbor, license_info, deprecated,
               request_parameters::text, first_observed_at, last_ingested_at
        FROM energy_datasets
        """;

    private static void AddFilter(NpgsqlCommand command, List<string> clauses, string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        clauses.Add($"{column} = @{column}");
        command.Parameters.AddWithValue(column, value);
    }

    private static string CreateDatasetId(DatasetMetadataDto metadata)
    {
        var parts = new[]
        {
            metadata.Source,
            metadata.Endpoint,
            metadata.Metric,
            metadata.Country,
            metadata.BiddingZone,
            metadata.Region,
            metadata.ProductionType,
            metadata.ForecastType,
            metadata.Neighbor,
            metadata.Granularity
        };

        return string.Join(':', parts
            .Select(x => string.IsNullOrWhiteSpace(x) ? "-" : x.Trim().ToLowerInvariant().Replace(' ', '-')));
    }

    private static void AddParameters(NpgsqlCommand command, DatasetMetadataDto dto)
    {
        command.Parameters.AddWithValue("id", dto.Id);
        command.Parameters.AddWithValue("curve_id", dto.CurveId);
        command.Parameters.AddWithValue("source", dto.Source);
        command.Parameters.AddWithValue("endpoint", dto.Endpoint);
        command.Parameters.AddWithValue("metric", dto.Metric);
        command.Parameters.AddWithValue("data_kind", dto.DataKind);
        command.Parameters.AddWithValue("category", dto.Category);
        command.Parameters.AddWithValue("unit", dto.Unit);
        command.Parameters.AddWithValue("country", dto.Country);
        command.Parameters.AddWithValue("bidding_zone", dto.BiddingZone);
        command.Parameters.AddWithValue("region", dto.Region);
        command.Parameters.AddWithValue("granularity", dto.Granularity);
        command.Parameters.AddWithValue("production_type", dto.ProductionType);
        command.Parameters.AddWithValue("forecast_type", dto.ForecastType);
        command.Parameters.AddWithValue("neighbor", dto.Neighbor);
        command.Parameters.AddWithValue("license_info", dto.LicenseInfo);
        command.Parameters.AddWithValue("deprecated", dto.Deprecated);
        command.Parameters.Add(new NpgsqlParameter("request_parameters", NpgsqlDbType.Jsonb) { Value = JsonSerializer.Serialize(dto.RequestParameters) });
        command.Parameters.AddWithValue("first_observed_at", dto.FirstObservedAt);
        command.Parameters.AddWithValue("last_ingested_at", dto.LastIngestedAt);
    }

    private static DatasetMetadataDto ToDto(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetString(0),
        CurveId = reader.GetString(1),
        Source = reader.GetString(2),
        Endpoint = reader.GetString(3),
        Metric = reader.GetString(4),
        DataKind = reader.GetString(5),
        Category = reader.GetString(6),
        Unit = reader.GetString(7),
        Country = reader.GetString(8),
        BiddingZone = reader.GetString(9),
        Region = reader.GetString(10),
        Granularity = reader.GetString(11),
        ProductionType = reader.GetString(12),
        ForecastType = reader.GetString(13),
        Neighbor = reader.GetString(14),
        LicenseInfo = reader.GetString(15),
        Deprecated = reader.GetBoolean(16),
        RequestParameters = JsonSerializer.Deserialize<Dictionary<string, string>>(reader.GetString(17)) ?? [],
        FirstObservedAt = reader.GetFieldValue<DateTimeOffset>(18),
        LastIngestedAt = reader.GetFieldValue<DateTimeOffset>(19)
    };
}
