using System.Web;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Ingestion.Services;

public static class EnergyChartsDefaults
{
    public static List<EnergyChartsDatasetDefinition> CreateDefinitions()
    {
        var definitions = new List<EnergyChartsDatasetDefinition>
        {
            Definition("/public_power", ("country", "dk")),
            Definition("/total_power", ("country", "de")),
            Definition("/installed_power", ("country", "dk"), ("time_step", "yearly")),
            Definition("/frequency", ("region", "DE-Freiburg")),
            Definition("/cbet", ("country", "dk")),
            Definition("/cbpf", ("country", "dk")),
            Definition("/signal", ("country", "dk")),
            Definition("/ren_share_forecast", ("country", "dk")),
            Definition("/ren_share_daily_avg", ("country", "dk")),
            Definition("/solar_share", ("country", "dk")),
            Definition("/solar_share_daily_avg", ("country", "dk")),
            Definition("/wind_onshore_share", ("country", "dk")),
            Definition("/wind_onshore_share_daily_avg", ("country", "dk")),
            Definition("/wind_offshore_share", ("country", "dk")),
            Definition("/wind_offshore_share_daily_avg", ("country", "dk"))
        };

        foreach (var bzn in new[] { "DK1", "DK2" })
        {
            definitions.Add(Definition("/price", ("bzn", bzn)));
        }

        foreach (var productionType in new[] { "solar", "wind_onshore", "wind_offshore", "load" })
        {
            var forecastTypes = productionType == "load"
                ? new[] { "day-ahead" }
                : new[] { "current", "intraday", "day-ahead" };

            foreach (var forecastType in forecastTypes)
            {
                definitions.Add(Definition(
                    "/public_power_forecast",
                    ("country", "dk"),
                    ("production_type", productionType),
                    ("forecast_type", forecastType)));
            }
        }

        return definitions;
    }

    public static EnergyChartsDatasetDefinition WithDateRange(EnergyChartsDatasetDefinition definition, int lookbackHours)
    {
        return WithDateRange(definition, $"now-{Math.Max(lookbackHours, 1)}h", "now");
    }

    public static EnergyChartsDatasetDefinition WithDateRange(EnergyChartsDatasetDefinition definition, string startExpression, string endExpression)
    {
        if (definition.Parameters.ContainsKey("start") || IsLatestOnly(definition.Endpoint))
        {
            return definition;
        }

        var copy = new EnergyChartsDatasetDefinition
        {
            Endpoint = definition.Endpoint,
            Parameters = new Dictionary<string, string>(definition.Parameters)
        };
        var now = DateTimeOffset.UtcNow;
        var start = DateTimeExpression.Resolve(startExpression, now);
        var end = DateTimeExpression.Resolve(endExpression, now);
        copy.Parameters["start"] = FormatTimestamp(start);
        copy.Parameters["end"] = FormatTimestamp(end);
        return copy;
    }

    public static string CreateDefinitionKey(EnergyChartsDatasetDefinition definition)
    {
        var query = string.Join('&', definition.Parameters.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
        return $"{definition.Endpoint}?{query}";
    }

    public static string ToQueryString(Dictionary<string, string> parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        var query = string.Join('&', parameters.Select(x =>
            $"{HttpUtility.UrlEncode(x.Key)}={HttpUtility.UrlEncode(x.Value)}"));
        return "?" + query;
    }

    private static EnergyChartsDatasetDefinition Definition(string endpoint, params (string Key, string Value)[] parameters) => new()
    {
        Endpoint = endpoint,
        Parameters = parameters.ToDictionary(x => x.Key, x => x.Value)
    };

    private static bool IsLatestOnly(string endpoint) =>
        endpoint.TrimStart('/') is "signal" or "ren_share_forecast" or "solar_share" or "wind_onshore_share" or "wind_offshore_share";

    private static string FormatTimestamp(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss'Z'");
}
