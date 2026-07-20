using System.Globalization;
using System.Text.Json;
using TimeLens.Domain.Models;

namespace TimeLens.Ingestion.Services;

public class EnergyChartsNormalizer
{
    public List<NormalizedDataset> Normalize(EnergyChartsDatasetDefinition definition, JsonElement root)
    {
        return definition.Endpoint switch
        {
            "/public_power" or "/total_power" => NormalizeNamedData(definition, root, "production_types", "MW"),
            "/public_power_forecast" => NormalizeForecast(definition, root),
            "/installed_power" => NormalizeInstalledPower(definition, root),
            "/price" => NormalizeArray(definition, root, "price", "price", GetString(root, "unit", "EUR/MWh")),
            "/cbet" or "/cbpf" => NormalizeNamedData(definition, root, "countries", "GW"),
            "/signal" => NormalizeSignal(definition, root),
            "/ren_share_forecast" => NormalizeRenewableShareForecast(definition, root),
            "/ren_share_daily_avg" or "/solar_share_daily_avg" or "/wind_onshore_share_daily_avg" or "/wind_offshore_share_daily_avg"
                => NormalizeDailyAverage(definition, root),
            "/solar_share" or "/wind_onshore_share" or "/wind_offshore_share" => NormalizeShare(definition, root),
            "/frequency" => NormalizeArray(definition, root, "data", "frequency", "Hz"),
            _ => []
        };
    }

    private static List<NormalizedDataset> NormalizeNamedData(EnergyChartsDatasetDefinition definition, JsonElement root, string property, string unit)
    {
        var timestamps = ReadUnixSeconds(root);
        var isExchange = definition.Endpoint is "/cbet" or "/cbpf";
        if (!root.TryGetProperty(property, out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<NormalizedDataset>();
        foreach (var item in items.EnumerateArray())
        {
            var name = GetString(item, "name");
            var values = ReadNullableDoubles(item, "data");
            result.Add(Create(definition, name, unit, timestamps, values, metadata =>
            {
                if (isExchange)
                {
                    metadata.Neighbor = name;
                }
                else
                {
                    metadata.ProductionType = name;
                }
            }, dataKind: isExchange ? "forecast" : "actual", category: isExchange ? "exchange" : "power"));
        }

        return result;
    }

    private static List<NormalizedDataset> NormalizeForecast(EnergyChartsDatasetDefinition definition, JsonElement root)
    {
        var productionType = GetString(root, "production_type", GetParameter(definition, "production_type"));
        var forecastType = GetString(root, "forecast_type", GetParameter(definition, "forecast_type"));
        return
        [
            Create(definition, "forecast", "MW", ReadUnixSeconds(root), ReadNullableDoubles(root, "forecast_values"), metadata =>
            {
                metadata.ProductionType = productionType;
                metadata.ForecastType = forecastType;
            }, dataKind: "forecast", category: "power")
        ];
    }

    private static List<NormalizedDataset> NormalizeInstalledPower(EnergyChartsDatasetDefinition definition, JsonElement root)
    {
        if (!root.TryGetProperty("time", out var time) ||
            !root.TryGetProperty("production_types", out var items) ||
            items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var timestamps = time.EnumerateArray()
            .Select(x => ParsePeriodStart(x.GetString()))
            .ToList();

        return items.EnumerateArray()
            .Select(item => Create(definition, "installed_power", "GW", timestamps, ReadNullableDoubles(item, "data"), metadata =>
            {
                metadata.ProductionType = GetString(item, "name");
                metadata.Granularity = GetParameter(definition, "time_step", "yearly");
            }, dataKind: "reference", category: "capacity"))
            .ToList();
    }

    private static List<NormalizedDataset> NormalizeSignal(EnergyChartsDatasetDefinition definition, JsonElement root)
    {
        var timestamps = ReadUnixSeconds(root);
        return
        [
            Create(definition, "renewable_share", "%", timestamps, ReadNullableDoubles(root, "share"), dataKind: "forecast", category: "share"),
            Create(definition, "traffic_signal", "signal", timestamps, ReadNullableDoubles(root, "signal"), dataKind: "forecast", category: "signal")
        ];
    }

    private static List<NormalizedDataset> NormalizeRenewableShareForecast(EnergyChartsDatasetDefinition definition, JsonElement root)
    {
        var timestamps = ReadUnixSeconds(root);
        return
        [
            Create(definition, "renewable_share_forecast", "%", timestamps, ReadNullableDoubles(root, "ren_share"), dataKind: "forecast", category: "share"),
            Create(definition, "solar_share_forecast", "%", timestamps, ReadNullableDoubles(root, "solar_share"), dataKind: "forecast", category: "share"),
            Create(definition, "wind_onshore_share_forecast", "%", timestamps, ReadNullableDoubles(root, "wind_onshore_share"), dataKind: "forecast", category: "share"),
            Create(definition, "wind_offshore_share_forecast", "%", timestamps, ReadNullableDoubles(root, "wind_offshore_share"), dataKind: "forecast", category: "share")
        ];
    }

    private static List<NormalizedDataset> NormalizeDailyAverage(EnergyChartsDatasetDefinition definition, JsonElement root)
    {
        if (!root.TryGetProperty("days", out var days))
        {
            return [];
        }

        var timestamps = days.EnumerateArray()
            .Select(x => DateTimeOffset.ParseExact(x.GetString() ?? string.Empty, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal))
            .ToList();

        return [Create(definition, definition.Endpoint.Trim('/'), "%", timestamps, ReadNullableDoubles(root, "data"), metadata => metadata.Granularity = "daily", category: "share")];
    }

    private static List<NormalizedDataset> NormalizeShare(EnergyChartsDatasetDefinition definition, JsonElement root)
    {
        var timestamps = ReadUnixSeconds(root);
        var metric = definition.Endpoint.Trim('/').Replace("_share", "_share");
        return
        [
            Create(definition, metric, "%", timestamps, ReadNullableDoubles(root, "data"), category: "share"),
            Create(definition, metric + "_forecast", "%", timestamps, ReadNullableDoubles(root, "forecast"), metadata => metadata.ForecastType = "forecast", dataKind: "forecast", category: "share")
        ];
    }

    private static List<NormalizedDataset> NormalizeArray(EnergyChartsDatasetDefinition definition, JsonElement root, string property, string metric, string unit) =>
        [Create(definition, metric, unit, ReadUnixSeconds(root), ReadNullableDoubles(root, property), category: ClassifyArray(metric))];

    private static NormalizedDataset Create(
        EnergyChartsDatasetDefinition definition,
        string metric,
        string unit,
        List<DateTimeOffset> timestamps,
        List<double?> values,
        Action<DatasetMetadataDto>? configure = null,
        string dataKind = "actual",
        string category = "unknown")
    {
        var metadata = new DatasetMetadataDto
        {
            Source = "energy-charts",
            Endpoint = definition.Endpoint.Trim('/'),
            Metric = metric,
            DataKind = dataKind,
            Category = category,
            Unit = unit,
            Country = GetParameter(definition, "country"),
            BiddingZone = GetParameter(definition, "bzn"),
            Region = GetParameter(definition, "region"),
            Granularity = InferGranularity(timestamps),
            LicenseInfo = string.Empty,
            Deprecated = false,
            RequestParameters = new Dictionary<string, string>(definition.Parameters)
        };
        configure?.Invoke(metadata);

        return new NormalizedDataset
        {
            Metadata = metadata,
            Batch = new TimeSeriesBatchRequest
            {
                Points = timestamps.Zip(values, (timestamp, value) => new TimeSeriesWritePoint
                {
                    Timestamp = timestamp,
                    Value = value
                }).ToList()
            }
        };
    }

    private static List<DateTimeOffset> ReadUnixSeconds(JsonElement root)
    {
        if (!root.TryGetProperty("unix_seconds", out var unixSeconds) || unixSeconds.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return unixSeconds.EnumerateArray()
            .Select(x => DateTimeOffset.FromUnixTimeSeconds(x.GetInt64()))
            .ToList();
    }

    private static List<double?> ReadNullableDoubles(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var values) || values.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return values.EnumerateArray()
            .Select(x => x.ValueKind == JsonValueKind.Null ? (double?)null : x.GetDouble())
            .ToList();
    }

    private static string GetString(JsonElement root, string property, string fallback = "") =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? fallback
            : fallback;

    private static string GetParameter(EnergyChartsDatasetDefinition definition, string key, string fallback = "") =>
        definition.Parameters.TryGetValue(key, out var value) ? value : fallback;

    private static DateTimeOffset ParsePeriodStart(string? value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        if (int.TryParse(value, out var year))
        {
            return new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }

        return DateTimeOffset.UtcNow;
    }

    private static string InferGranularity(List<DateTimeOffset> timestamps)
    {
        if (timestamps.Count < 2)
        {
            return "unknown";
        }

        var delta = timestamps[1] - timestamps[0];
        if (delta.TotalSeconds <= 1) return "second";
        if (delta.TotalMinutes <= 15) return "quarter-hour";
        if (delta.TotalHours <= 1) return "hourly";
        if (delta.TotalDays <= 1) return "daily";
        return "periodic";
    }

    private static string ClassifyArray(string metric) => metric switch
    {
        "price" => "price",
        "frequency" => "frequency",
        _ => "unknown"
    };
}
