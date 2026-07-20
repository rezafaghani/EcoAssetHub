using System.Text.Json;
using TimeLens.Domain.Models;
using TimeLens.Domain.Services;

namespace TimeLens.Validation;

public static class QualityValidationEngine
{
    public static List<QualityFindingDraftDto> Evaluate(QualityEvaluationRequest request)
    {
        Validate(request);
        var context = new ExecutionPluginContext(
            "dataset",
            request.Metadata.Id,
            request.Start,
            request.End,
            JsonSerializer.SerializeToElement(new
            {
                request.AllowedDelay,
                request.MinimumValue,
                request.MaximumValue,
                request.MaximumAbsoluteChange,
                request.MaximumPercentageChange,
                request.NearZeroFloor,
                request.FlatLinePointCount
            }, JsonOptions),
            request.Metadata,
            request.Points);

        return ValidationPlugins.All
            .SelectMany(plugin => plugin.Evaluate(context, request))
            .ToList();
    }

    public static List<DateTimeOffset> ExpectedTimestamps(DateTimeOffset start, DateTimeOffset end, TimeSpan granularity)
    {
        if (start >= end || granularity <= TimeSpan.Zero)
        {
            return [];
        }

        var result = new List<DateTimeOffset>();
        for (var timestamp = start; timestamp < end; timestamp = timestamp.Add(granularity))
        {
            result.Add(timestamp);
        }

        return result;
    }

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static void Validate(QualityEvaluationRequest request)
    {
        if (request.Start >= request.End)
        {
            throw new ArgumentException("Quality evaluation requires a half-open range where start is before end.");
        }

        if (request.Granularity <= TimeSpan.Zero)
        {
            throw new ArgumentException("Quality evaluation requires a positive granularity.");
        }
    }

    internal static QualityFindingDraftDto Finding(
        string validatorId,
        string category,
        string severity,
        string status,
        string title,
        string message,
        DateTimeOffset? affectedStart,
        DateTimeOffset? affectedEnd,
        int? expectedCount = null,
        int? actualCount = null,
        int? affectedCount = null,
        IEnumerable<DateTimeOffset>? samples = null) =>
        new(validatorId, category, severity, status, title, message, affectedStart, affectedEnd, expectedCount, actualCount, affectedCount, samples?.ToList() ?? []);
}

public abstract class QualityValidationPlugin(
    string validatorId,
    string category,
    string name,
    string description,
    string defaultSeverity,
    object defaultConfiguration) : IExecutionPlugin
{
    public const string IdPrefix = "timelens.validation.";
    public string ValidatorId { get; } = validatorId;
    public string Category { get; } = category;
    public string DefaultSeverity { get; } = defaultSeverity;

    public ExecutionPluginDto Metadata { get; } = new(
        IdPrefix + validatorId,
        name,
        description,
        ExecutionCategories.Validation,
        1,
        ["dataset"],
        [],
        [],
        JsonSerializer.SerializeToElement(defaultConfiguration, QualityValidationEngine.JsonOptions),
        JsonSerializer.SerializeToElement(defaultConfiguration, QualityValidationEngine.JsonOptions),
        "quality.finding");

    public Task<List<ExecutionStepResultDto>> ExecuteAsync(ExecutionPluginContext context, CancellationToken cancellationToken = default)
    {
        if (context.Dataset is null)
        {
            throw new ArgumentException($"Dataset '{context.TargetId}' was not found.");
        }

        if (!ExecutionPluginConfiguration.TryGetDuration(context.Configuration, "granularity", out var granularity)
            && !ExecutionPluginConfiguration.TryParseDuration(context.Dataset.Granularity, out granularity))
        {
            throw new ArgumentException($"Dataset '{context.TargetId}' has invalid granularity.");
        }

        var request = new QualityEvaluationRequest(
            context.Dataset,
            context.Start,
            context.End,
            DateTimeOffset.UtcNow,
            granularity,
            ExecutionPluginConfiguration.GetOptionalDuration(context.Configuration, "allowedDelay"),
            ExecutionPluginConfiguration.GetOptionalDouble(context.Configuration, "minimumValue", "min"),
            ExecutionPluginConfiguration.GetOptionalDouble(context.Configuration, "maximumValue", "max"),
            ExecutionPluginConfiguration.GetOptionalDouble(context.Configuration, "maximumAbsoluteChange", "maxAbsoluteChange"),
            ExecutionPluginConfiguration.GetOptionalDouble(context.Configuration, "maximumPercentageChange", "maxPercentageChange"),
            ExecutionPluginConfiguration.GetOptionalDouble(context.Configuration, "nearZeroFloor") ?? 0.000001,
            ExecutionPluginConfiguration.GetOptionalInt(context.Configuration, "flatLinePointCount") ?? 0,
            context.Points);
        QualityValidationEngine.Validate(request);

        var results = Evaluate(context, request)
            .Select(finding => new ExecutionStepResultDto(
                Metadata.Id,
                context.TargetId,
                finding.QualityStatus,
                Metadata.ResultType,
                finding.Title,
                JsonSerializer.SerializeToElement(new
                {
                    finding.ExpectedCount,
                    finding.ActualCount,
                    finding.AffectedCount
                }, QualityValidationEngine.JsonOptions),
                JsonSerializer.SerializeToElement(finding, QualityValidationEngine.JsonOptions)))
            .ToList();
        return Task.FromResult(results);
    }

    internal abstract List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request);

    protected static List<TimeSeriesPointDto> Points(QualityEvaluationRequest request) =>
        request.Points.OrderBy(x => x.Timestamp).ToList();
}

public sealed class RequiredMetadataValidationPlugin() : QualityValidationPlugin(
    "metadata.required",
    "metadata",
    "Required metadata",
    "Checks required curve metadata such as provider, type, granularity, unit, and time zone.",
    "warning",
    new { requiredFields = new[] { "source", "dataKind", "category", "granularity", "unit" } })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Metadata.Source)) missing.Add("source");
        if (string.IsNullOrWhiteSpace(request.Metadata.DataKind)) missing.Add("dataKind");
        if (string.IsNullOrWhiteSpace(request.Metadata.Category)) missing.Add("category");
        if (string.IsNullOrWhiteSpace(request.Metadata.Granularity)) missing.Add("granularity");
        if (string.IsNullOrWhiteSpace(request.Metadata.Unit)) missing.Add("unit");

        return missing.Count == 0 ? [] :
        [
            QualityValidationEngine.Finding("metadata.required", "metadata", "warning", QualityStatuses.Degraded,
                "Required metadata is missing",
                $"Missing metadata: {string.Join(", ", missing)}.",
                request.Start, request.End, affectedCount: missing.Count)
        ];
    }
}

public sealed class EmptyDataValidationPlugin() : QualityValidationPlugin(
    "availability.empty-data",
    "availability",
    "Empty data",
    "Checks whether the requested evaluation window returns any usable data.",
    "critical",
    new { })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        var points = Points(request);
        return points.Count != 0 ? [] :
        [
            QualityValidationEngine.Finding("availability.empty-data", "availability", "critical", QualityStatuses.Critical,
                "No data returned",
                "The evaluation range returned no time-series points.",
                request.Start, request.End, expectedCount: QualityValidationEngine.ExpectedTimestamps(request.Start, request.End, request.Granularity).Count, actualCount: 0)
        ];
    }
}

public sealed class MissingTimestampsValidationPlugin() : QualityValidationPlugin(
    "completeness.missing-timestamps",
    "completeness",
    "Missing timestamps",
    "Checks expected timestamps over a half-open evaluation range.",
    "warning",
    new { granularity = "PT15M", maxMissingRatio = 0.0 })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        var expected = QualityValidationEngine.ExpectedTimestamps(request.Start, request.End, request.Granularity);
        var actual = Points(request)
            .Where(x => x.Timestamp >= request.Start && x.Timestamp < request.End)
            .Select(x => x.Timestamp)
            .ToHashSet();
        var missing = expected.Where(x => !actual.Contains(x)).ToList();

        return missing.Count == 0 ? [] :
        [
            QualityValidationEngine.Finding("completeness.missing-timestamps", "completeness", "warning", QualityStatuses.Degraded,
                "Expected timestamps are missing",
                $"{missing.Count} of {expected.Count} expected timestamps are missing.",
                missing.First(), missing.Last().Add(request.Granularity), expected.Count, actual.Count, missing.Count, missing.Take(20))
        ];
    }
}

public sealed class TimestampAlignmentValidationPlugin() : QualityValidationPlugin(
    "timestamps.alignment",
    "timestamp_alignment",
    "Timestamp alignment",
    "Checks that timestamps align to the configured curve granularity.",
    "warning",
    new { granularity = "PT15M" })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        var misaligned = Points(request)
            .Where(x => x.Timestamp >= request.Start && x.Timestamp < request.End)
            .Where(x => ((x.Timestamp - request.Start).Ticks % request.Granularity.Ticks) != 0)
            .Select(x => x.Timestamp)
            .ToList();

        return misaligned.Count == 0 ? [] :
        [
            QualityValidationEngine.Finding("timestamps.alignment", "timestamp_alignment", "warning", QualityStatuses.Degraded,
                "Timestamps are misaligned",
                $"{misaligned.Count} timestamps do not align to {request.Granularity}.",
                misaligned.First(), misaligned.Last(), affectedCount: misaligned.Count, samples: misaligned.Take(20))
        ];
    }
}

public sealed class FreshnessValidationPlugin() : QualityValidationPlugin(
    "freshness.latest-point",
    "freshness",
    "Latest point freshness",
    "Checks latest data against allowed delay, grace period, and publication expectations.",
    "critical",
    new { allowedDelay = "PT30M", gracePeriod = "PT10M", timeZone = "UTC" })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        var points = Points(request);
        if (request.AllowedDelay is null || points.Count == 0)
        {
            return [];
        }

        var latest = points.Max(x => x.Timestamp);
        var delay = request.Now - latest;
        return delay <= request.AllowedDelay.Value ? [] :
        [
            QualityValidationEngine.Finding("freshness.latest-point", "freshness", "critical", QualityStatuses.Critical,
                "Latest point is stale",
                $"Latest point is {delay} old; allowed delay is {request.AllowedDelay.Value}.",
                latest, request.Now, affectedCount: 1, samples: [latest])
        ];
    }
}

public sealed class DuplicateTimestampsValidationPlugin() : QualityValidationPlugin(
    "duplicates.timestamp-conflict",
    "duplicates",
    "Duplicate timestamps",
    "Checks duplicate timestamps and conflicting values when raw duplicate-preserving data is available.",
    "warning",
    new { })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        var duplicates = Points(request)
            .GroupBy(x => x.Timestamp)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        return duplicates.Count == 0 ? [] :
        [
            QualityValidationEngine.Finding("duplicates.timestamp-conflict", "duplicates", "warning", QualityStatuses.Degraded,
                "Duplicate timestamps detected",
                $"{duplicates.Count} timestamps have multiple records.",
                duplicates.First(), duplicates.Last(), affectedCount: duplicates.Count, samples: duplicates.Take(20))
        ];
    }
}

public sealed class ValueRangeValidationPlugin() : QualityValidationPlugin(
    "validity.value-range",
    "value_validity",
    "Value validity",
    "Checks null, invalid numeric values, and configured min/max bounds.",
    "warning",
    new { min = (double?)null, max = (double?)null, allowNegative = true, allowZero = true })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        var invalid = Points(request)
            .Where(x => x.Timestamp >= request.Start && x.Timestamp < request.End)
            .Where(x => x.Value is null
                || double.IsNaN(x.Value.Value)
                || double.IsInfinity(x.Value.Value)
                || request.MinimumValue.HasValue && x.Value.Value < request.MinimumValue.Value
                || request.MaximumValue.HasValue && x.Value.Value > request.MaximumValue.Value)
            .Select(x => x.Timestamp)
            .ToList();

        return invalid.Count == 0 ? [] :
        [
            QualityValidationEngine.Finding("validity.value-range", "value_validity", "warning", QualityStatuses.Degraded,
                "Values are invalid",
                $"{invalid.Count} values are null, non-finite, or outside configured bounds.",
                invalid.First(), invalid.Last(), affectedCount: invalid.Count, samples: invalid.Take(20))
        ];
    }
}

public sealed class RateOfChangeValidationPlugin() : QualityValidationPlugin(
    "continuity.rate-of-change",
    "continuity",
    "Rate of change",
    "Checks maximum absolute and percentage change between consecutive values.",
    "warning",
    new { maxAbsoluteChange = (double?)null, maxPercentageChange = (double?)null, nearZeroFloor = 0.000001 })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        if (request.MaximumAbsoluteChange is null && request.MaximumPercentageChange is null)
        {
            return [];
        }

        var bad = new List<DateTimeOffset>();
        var finite = Points(request).Where(x => x.Value.HasValue && !double.IsNaN(x.Value.Value) && !double.IsInfinity(x.Value.Value)).ToList();
        for (var i = 1; i < finite.Count; i++)
        {
            var previous = finite[i - 1].Value!.Value;
            var current = finite[i].Value!.Value;
            var absolute = Math.Abs(current - previous);
            var denominator = Math.Max(Math.Abs(previous), request.NearZeroFloor);
            var percentage = absolute / denominator * 100;

            if (request.MaximumAbsoluteChange.HasValue && absolute > request.MaximumAbsoluteChange.Value
                || request.MaximumPercentageChange.HasValue && percentage > request.MaximumPercentageChange.Value)
            {
                bad.Add(finite[i].Timestamp);
            }
        }

        return bad.Count == 0 ? [] :
        [
            QualityValidationEngine.Finding("continuity.rate-of-change", "continuity", "warning", QualityStatuses.Degraded,
                "Rate of change exceeded",
                $"{bad.Count} consecutive changes exceed configured limits.",
                bad.First(), bad.Last(), affectedCount: bad.Count, samples: bad.Take(20))
        ];
    }
}

public sealed class FlatLineValidationPlugin() : QualityValidationPlugin(
    "stale.flat-line",
    "stale_values",
    "Flat line",
    "Checks repeated or near-constant values over a configured window.",
    "warning",
    new { window = "PT2H", varianceThreshold = 0.0 })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request)
    {
        if (request.FlatLinePointCount < 2)
        {
            return [];
        }

        var finite = Points(request).Where(x => x.Value.HasValue && !double.IsNaN(x.Value.Value) && !double.IsInfinity(x.Value.Value)).ToList();
        var run = new List<DateTimeOffset>();
        for (var i = 1; i < finite.Count; i++)
        {
            if (finite[i].Value == finite[i - 1].Value)
            {
                if (run.Count == 0)
                {
                    run.Add(finite[i - 1].Timestamp);
                }

                run.Add(finite[i].Timestamp);
                continue;
            }

            if (run.Count >= request.FlatLinePointCount)
            {
                break;
            }

            run.Clear();
        }

        return run.Count < request.FlatLinePointCount ? [] :
        [
            QualityValidationEngine.Finding("stale.flat-line", "stale_values", "warning", QualityStatuses.Degraded,
                "Values are flat",
                $"{run.Count} consecutive values are unchanged.",
                run.First(), run.Last(), affectedCount: run.Count, samples: run.Take(20))
        ];
    }
}

public sealed class RollingThresholdAnomalyValidationPlugin() : QualityValidationPlugin(
    "anomaly.rolling-threshold",
    "anomaly",
    "Rolling threshold anomaly",
    "Checks basic deterministic spikes or drops against a rolling window.",
    "informational",
    new { lookback = "P7D", standardDeviationMultiplier = 3.0 })
{
    internal override List<QualityFindingDraftDto> Evaluate(ExecutionPluginContext context, QualityEvaluationRequest request) => [];
}

internal static class ValidationPlugins
{
    public static readonly List<QualityValidationPlugin> All =
    [
        new RequiredMetadataValidationPlugin(),
        new EmptyDataValidationPlugin(),
        new MissingTimestampsValidationPlugin(),
        new TimestampAlignmentValidationPlugin(),
        new FreshnessValidationPlugin(),
        new DuplicateTimestampsValidationPlugin(),
        new ValueRangeValidationPlugin(),
        new RateOfChangeValidationPlugin(),
        new FlatLineValidationPlugin(),
        new RollingThresholdAnomalyValidationPlugin()
    ];
}
