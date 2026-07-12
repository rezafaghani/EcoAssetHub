using EcoAssetHub.Domain.Entities;

namespace EcoAssetHub.Scheduler;

public static class DefaultSchedules
{
    private const string EveryTenMinutes = "*/10 * * * *";
    private const string EveryThirtyMinutes = "*/30 * * * *";
    private const string TwiceDaily = "0 2,14 * * *";
    private const string Daily = "0 3 * * *";

    public static IReadOnlyCollection<IngestionSchedule> Create()
    {
        var schedules = new List<IngestionSchedule>
        {
            Schedule("energycharts-public-power-dk", "dk.public_power", "/public_power", EveryTenMinutes, ("country", "dk")),
            Schedule("energycharts-total-power-de", "de.total_power", "/total_power", EveryTenMinutes, ("country", "de")),
            Schedule("energycharts-installed-power-dk", "dk.installed_power", "/installed_power", Daily, ("country", "dk"), ("time_step", "yearly")),
            Schedule("energycharts-cbet-dk", "dk.cbet", "/cbet", EveryTenMinutes, ("country", "dk")),
            Schedule("energycharts-cbpf-dk", "dk.cbpf", "/cbpf", EveryTenMinutes, ("country", "dk")),
            Schedule("energycharts-signal-dk", "dk.signal", "/signal", EveryTenMinutes, ("country", "dk")),
            Schedule("energycharts-ren-share-forecast-dk", "dk.ren_share_forecast", "/ren_share_forecast", EveryThirtyMinutes, ("country", "dk")),
            Schedule("energycharts-ren-share-daily-dk", "dk.ren_share_daily_avg", "/ren_share_daily_avg", TwiceDaily, ("country", "dk")),
            Schedule("energycharts-solar-share-dk", "dk.solar_share", "/solar_share", EveryTenMinutes, ("country", "dk")),
            Schedule("energycharts-solar-share-daily-dk", "dk.solar_share_daily_avg", "/solar_share_daily_avg", TwiceDaily, ("country", "dk")),
            Schedule("energycharts-wind-onshore-share-dk", "dk.wind_onshore_share", "/wind_onshore_share", EveryTenMinutes, ("country", "dk")),
            Schedule("energycharts-wind-onshore-share-daily-dk", "dk.wind_onshore_share_daily_avg", "/wind_onshore_share_daily_avg", TwiceDaily, ("country", "dk")),
            Schedule("energycharts-wind-offshore-share-dk", "dk.wind_offshore_share", "/wind_offshore_share", EveryTenMinutes, ("country", "dk")),
            Schedule("energycharts-wind-offshore-share-daily-dk", "dk.wind_offshore_share_daily_avg", "/wind_offshore_share_daily_avg", TwiceDaily, ("country", "dk")),
            Schedule("energycharts-frequency-de-freiburg", "de.frequency", "/frequency", EveryTenMinutes, ("region", "DE-Freiburg"))
        };

        foreach (var bzn in new[] { "DK1", "DK2" })
        {
            schedules.Add(Schedule($"energycharts-price-{bzn.ToLowerInvariant()}", $"{bzn}.price", "/price", EveryThirtyMinutes, ("bzn", bzn)));
        }

        foreach (var productionType in new[] { "solar", "wind_onshore", "wind_offshore", "load" })
        {
            var forecastTypes = productionType == "load"
                ? new[] { "day-ahead" }
                : new[] { "current", "intraday", "day-ahead" };

            foreach (var forecastType in forecastTypes)
            {
                schedules.Add(Schedule(
                    $"energycharts-forecast-{productionType}-{forecastType}",
                    $"dk.forecast.{productionType}.{forecastType}",
                    "/public_power_forecast",
                    GetForecastCron(forecastType),
                    ("country", "dk"),
                    ("production_type", productionType),
                    ("forecast_type", forecastType)));
            }
        }

        return schedules;
    }

    private static IngestionSchedule Schedule(string id, string curveId, string endpoint, string cron, params (string Key, string Value)[] parameters) => new()
    {
        Id = id,
        Name = id,
        CurveId = curveId,
        CronExpression = cron,
        Endpoint = endpoint,
        Parameters = parameters.ToDictionary(x => x.Key, x => x.Value),
        LookbackHours = 48,
        BatchSize = 500
    };

    private static string GetForecastCron(string forecastType) =>
        forecastType == "current" ? EveryTenMinutes : EveryThirtyMinutes;
}
