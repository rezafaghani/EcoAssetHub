using System.Text.Json;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.API.Infrastructure.Services;

public class QualityValidatorCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public List<QualityValidatorTypeDto> List() =>
    [
        Type("metadata.required", "metadata", "Required metadata", "Checks required curve metadata such as provider, type, granularity, unit, and time zone.", "curve", "warning", new
        {
            requiredFields = new[] { "source", "dataKind", "category", "granularity", "unit" }
        }),
        Type("availability.empty-data", "availability", "Empty data", "Checks whether the requested evaluation window returns any usable data.", "curve", "critical", new { }),
        Type("completeness.missing-timestamps", "completeness", "Missing timestamps", "Checks expected timestamps over a half-open evaluation range.", "curve", "warning", new
        {
            granularity = "PT15M",
            maxMissingRatio = 0.0
        }),
        Type("timestamps.alignment", "timestamp_alignment", "Timestamp alignment", "Checks that timestamps align to the configured curve granularity.", "curve", "warning", new
        {
            granularity = "PT15M"
        }),
        Type("freshness.latest-point", "freshness", "Latest point freshness", "Checks latest data against allowed delay, grace period, and publication expectations.", "curve", "critical", new
        {
            allowedDelay = "PT30M",
            gracePeriod = "PT10M",
            timeZone = "UTC"
        }),
        Type("duplicates.timestamp-conflict", "duplicates", "Duplicate timestamps", "Checks duplicate timestamps and conflicting values when raw duplicate-preserving data is available.", "curve", "warning", new { }),
        Type("validity.value-range", "value_validity", "Value validity", "Checks null, invalid numeric values, and configured min/max bounds.", "curve", "warning", new
        {
            min = (double?)null,
            max = (double?)null,
            allowNegative = true,
            allowZero = true
        }),
        Type("continuity.rate-of-change", "continuity", "Rate of change", "Checks maximum absolute and percentage change between consecutive values.", "curve", "warning", new
        {
            maxAbsoluteChange = (double?)null,
            maxPercentageChange = (double?)null,
            nearZeroFloor = 0.000001
        }),
        Type("stale.flat-line", "stale_values", "Flat line", "Checks repeated or near-constant values over a configured window.", "curve", "warning", new
        {
            window = "PT2H",
            varianceThreshold = 0.0
        }),
        Type("anomaly.rolling-threshold", "anomaly", "Rolling threshold anomaly", "Checks basic deterministic spikes or drops against a rolling window.", "curve", "informational", new
        {
            lookback = "P7D",
            standardDeviationMultiplier = 3.0
        })
    ];

    private static QualityValidatorTypeDto Type(
        string id,
        string category,
        string displayName,
        string description,
        string targetType,
        string defaultSeverity,
        object config)
    {
        var schema = JsonSerializer.SerializeToElement(config, JsonOptions);
        return new QualityValidatorTypeDto(id, category, displayName, description, targetType, 1, defaultSeverity, schema);
    }
}
