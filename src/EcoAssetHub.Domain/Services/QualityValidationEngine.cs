using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Domain.Services;

public static class QualityValidationEngine
{
    public static List<QualityFindingDraftDto> Evaluate(QualityEvaluationRequest request)
    {
        if (request.Start >= request.End)
        {
            throw new ArgumentException("Quality evaluation requires a half-open range where start is before end.");
        }

        if (request.Granularity <= TimeSpan.Zero)
        {
            throw new ArgumentException("Quality evaluation requires a positive granularity.");
        }

        var points = request.Points.OrderBy(x => x.Timestamp).ToList();
        var findings = new List<QualityFindingDraftDto>();

        AddMetadataFindings(request, findings);
        AddAvailabilityFinding(request, points, findings);
        AddCompletenessFinding(request, points, findings);
        AddAlignmentFinding(request, points, findings);
        AddFreshnessFinding(request, points, findings);
        AddValueValidityFinding(request, points, findings);
        AddDuplicateFinding(points, findings);
        AddRateOfChangeFinding(request, points, findings);
        AddFlatLineFinding(request, points, findings);

        return findings;
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

    private static void AddMetadataFindings(QualityEvaluationRequest request, List<QualityFindingDraftDto> findings)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(request.Metadata.Source)) missing.Add("source");
        if (string.IsNullOrWhiteSpace(request.Metadata.DataKind)) missing.Add("dataKind");
        if (string.IsNullOrWhiteSpace(request.Metadata.Category)) missing.Add("category");
        if (string.IsNullOrWhiteSpace(request.Metadata.Granularity)) missing.Add("granularity");
        if (string.IsNullOrWhiteSpace(request.Metadata.Unit)) missing.Add("unit");

        if (missing.Count > 0)
        {
            findings.Add(Finding("metadata.required", "metadata", "warning", QualityStatuses.Degraded,
                "Required metadata is missing",
                $"Missing metadata: {string.Join(", ", missing)}.",
                request.Start, request.End, affectedCount: missing.Count));
        }
    }

    private static void AddAvailabilityFinding(QualityEvaluationRequest request, List<TimeSeriesPointDto> points, List<QualityFindingDraftDto> findings)
    {
        if (points.Count == 0)
        {
            findings.Add(Finding("availability.empty-data", "availability", "critical", QualityStatuses.Critical,
                "No data returned",
                "The evaluation range returned no time-series points.",
                request.Start, request.End, expectedCount: ExpectedTimestamps(request.Start, request.End, request.Granularity).Count, actualCount: 0));
        }
    }

    private static void AddCompletenessFinding(QualityEvaluationRequest request, List<TimeSeriesPointDto> points, List<QualityFindingDraftDto> findings)
    {
        var expected = ExpectedTimestamps(request.Start, request.End, request.Granularity);
        var actual = points
            .Where(x => x.Timestamp >= request.Start && x.Timestamp < request.End)
            .Select(x => x.Timestamp)
            .ToHashSet();
        var missing = expected.Where(x => !actual.Contains(x)).ToList();

        if (missing.Count > 0)
        {
            findings.Add(Finding("completeness.missing-timestamps", "completeness", "warning", QualityStatuses.Degraded,
                "Expected timestamps are missing",
                $"{missing.Count} of {expected.Count} expected timestamps are missing.",
                missing.First(), missing.Last().Add(request.Granularity), expected.Count, actual.Count, missing.Count, missing.Take(20)));
        }
    }

    private static void AddAlignmentFinding(QualityEvaluationRequest request, List<TimeSeriesPointDto> points, List<QualityFindingDraftDto> findings)
    {
        var misaligned = points
            .Where(x => x.Timestamp >= request.Start && x.Timestamp < request.End)
            .Where(x => ((x.Timestamp - request.Start).Ticks % request.Granularity.Ticks) != 0)
            .Select(x => x.Timestamp)
            .ToList();

        if (misaligned.Count > 0)
        {
            findings.Add(Finding("timestamps.alignment", "timestamp_alignment", "warning", QualityStatuses.Degraded,
                "Timestamps are misaligned",
                $"{misaligned.Count} timestamps do not align to {request.Granularity}.",
                misaligned.First(), misaligned.Last(), affectedCount: misaligned.Count, samples: misaligned.Take(20)));
        }
    }

    private static void AddFreshnessFinding(QualityEvaluationRequest request, List<TimeSeriesPointDto> points, List<QualityFindingDraftDto> findings)
    {
        if (request.AllowedDelay is null || points.Count == 0)
        {
            return;
        }

        var latest = points.Max(x => x.Timestamp);
        var delay = request.Now - latest;
        if (delay > request.AllowedDelay.Value)
        {
            findings.Add(Finding("freshness.latest-point", "freshness", "critical", QualityStatuses.Critical,
                "Latest point is stale",
                $"Latest point is {delay} old; allowed delay is {request.AllowedDelay.Value}.",
                latest, request.Now, affectedCount: 1, samples: [latest]));
        }
    }

    private static void AddValueValidityFinding(QualityEvaluationRequest request, List<TimeSeriesPointDto> points, List<QualityFindingDraftDto> findings)
    {
        var invalid = points
            .Where(x => x.Timestamp >= request.Start && x.Timestamp < request.End)
            .Where(x => x.Value is null
                || double.IsNaN(x.Value.Value)
                || double.IsInfinity(x.Value.Value)
                || request.MinimumValue.HasValue && x.Value.Value < request.MinimumValue.Value
                || request.MaximumValue.HasValue && x.Value.Value > request.MaximumValue.Value)
            .Select(x => x.Timestamp)
            .ToList();

        if (invalid.Count > 0)
        {
            findings.Add(Finding("validity.value-range", "value_validity", "warning", QualityStatuses.Degraded,
                "Values are invalid",
                $"{invalid.Count} values are null, non-finite, or outside configured bounds.",
                invalid.First(), invalid.Last(), affectedCount: invalid.Count, samples: invalid.Take(20)));
        }
    }

    private static void AddDuplicateFinding(List<TimeSeriesPointDto> points, List<QualityFindingDraftDto> findings)
    {
        var duplicates = points
            .GroupBy(x => x.Timestamp)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            findings.Add(Finding("duplicates.timestamp-conflict", "duplicates", "warning", QualityStatuses.Degraded,
                "Duplicate timestamps detected",
                $"{duplicates.Count} timestamps have multiple records.",
                duplicates.First(), duplicates.Last(), affectedCount: duplicates.Count, samples: duplicates.Take(20)));
        }
    }

    private static void AddRateOfChangeFinding(QualityEvaluationRequest request, List<TimeSeriesPointDto> points, List<QualityFindingDraftDto> findings)
    {
        if (request.MaximumAbsoluteChange is null && request.MaximumPercentageChange is null)
        {
            return;
        }

        var bad = new List<DateTimeOffset>();
        var finite = points.Where(x => x.Value.HasValue && !double.IsNaN(x.Value.Value) && !double.IsInfinity(x.Value.Value)).ToList();
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

        if (bad.Count > 0)
        {
            findings.Add(Finding("continuity.rate-of-change", "continuity", "warning", QualityStatuses.Degraded,
                "Rate of change exceeded",
                $"{bad.Count} consecutive changes exceed configured limits.",
                bad.First(), bad.Last(), affectedCount: bad.Count, samples: bad.Take(20)));
        }
    }

    private static void AddFlatLineFinding(QualityEvaluationRequest request, List<TimeSeriesPointDto> points, List<QualityFindingDraftDto> findings)
    {
        if (request.FlatLinePointCount < 2)
        {
            return;
        }

        var finite = points.Where(x => x.Value.HasValue && !double.IsNaN(x.Value.Value) && !double.IsInfinity(x.Value.Value)).ToList();
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

        if (run.Count >= request.FlatLinePointCount)
        {
            findings.Add(Finding("stale.flat-line", "stale_values", "warning", QualityStatuses.Degraded,
                "Values are flat",
                $"{run.Count} consecutive values are unchanged.",
                run.First(), run.Last(), affectedCount: run.Count, samples: run.Take(20)));
        }
    }

    private static QualityFindingDraftDto Finding(
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
