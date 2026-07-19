using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Scheduler;

public static class DefaultDatasetMetadata
{
    public static IEnumerable<DatasetMetadataDto> Create(IEnumerable<IngestionSchedule> schedules)
    {
        foreach (var schedule in schedules)
        {
            yield return new DatasetMetadataDto
            {
                CurveId = schedule.CurveId,
                Source = schedule.Source,
                Endpoint = schedule.Endpoint.Trim('/'),
                Metric = Metric(schedule),
                DataKind = DataKind(schedule.Endpoint),
                Category = Category(schedule.Endpoint),
                Country = Parameter(schedule, "country"),
                BiddingZone = Parameter(schedule, "bzn"),
                Region = Parameter(schedule, "region"),
                Granularity = "unknown",
                ProductionType = Parameter(schedule, "production_type"),
                ForecastType = Parameter(schedule, "forecast_type"),
                RequestParameters = new Dictionary<string, string>(schedule.Parameters)
            };
        }
    }

    private static string Metric(IngestionSchedule schedule) =>
        schedule.Endpoint.Trim('/') == "public_power_forecast"
            ? "forecast"
            : schedule.Endpoint.Trim('/');

    private static string DataKind(string endpoint) => endpoint switch
    {
        "/public_power_forecast" or "/cbet" or "/cbpf" or "/signal" or "/ren_share_forecast" => "forecast",
        "/installed_power" => "reference",
        _ => "actual"
    };

    private static string Category(string endpoint) => endpoint switch
    {
        "/public_power" or "/total_power" or "/public_power_forecast" => "power",
        "/installed_power" => "capacity",
        "/price" => "price",
        "/cbet" or "/cbpf" => "exchange",
        "/frequency" => "frequency",
        "/signal" => "signal",
        _ when endpoint.Contains("share", StringComparison.OrdinalIgnoreCase) => "share",
        _ => "unknown"
    };

    private static string Parameter(IngestionSchedule schedule, string key) =>
        schedule.Parameters.TryGetValue(key, out var value) ? value : string.Empty;
}
