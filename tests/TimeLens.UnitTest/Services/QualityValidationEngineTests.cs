using TimeLens.Domain.Models;
using TimeLens.Validation;

namespace TimeLens.UnitTest.Services;

public class QualityValidationEngineTests
{
    [Fact]
    public void ExpectedTimestamps_UsesHalfOpenRange()
    {
        var start = DateTimeOffset.Parse("2026-01-01T10:00:00Z");
        var end = DateTimeOffset.Parse("2026-01-01T11:00:00Z");

        var expected = QualityValidationEngine.ExpectedTimestamps(start, end, TimeSpan.FromMinutes(15));

        Assert.Equal([
            DateTimeOffset.Parse("2026-01-01T10:00:00Z"),
            DateTimeOffset.Parse("2026-01-01T10:15:00Z"),
            DateTimeOffset.Parse("2026-01-01T10:30:00Z"),
            DateTimeOffset.Parse("2026-01-01T10:45:00Z")
        ], expected);
    }

    [Fact]
    public void Evaluate_DetectsMissingAndMisalignedTimestampsSeparately()
    {
        var request = Request([
            Point("2026-01-01T10:00:00Z", 1),
            Point("2026-01-01T10:07:00Z", 2),
            Point("2026-01-01T10:30:00Z", 3),
            Point("2026-01-01T10:45:00Z", 4)
        ]);

        var findings = QualityValidationEngine.Evaluate(request);

        Assert.Contains(findings, x => x.ValidatorId == "completeness.missing-timestamps" && x.AffectedCount == 1);
        Assert.Contains(findings, x => x.ValidatorId == "timestamps.alignment" && x.AffectedCount == 1);
    }

    [Fact]
    public void Evaluate_DetectsFreshnessAndDuplicateAndInvalidValues()
    {
        var request = Request([
            Point("2026-01-01T10:00:00Z", 1),
            Point("2026-01-01T10:00:00Z", 2),
            Point("2026-01-01T10:15:00Z", null)
        ]) with
        {
            Now = DateTimeOffset.Parse("2026-01-01T12:00:00Z"),
            AllowedDelay = TimeSpan.FromMinutes(30)
        };

        var findings = QualityValidationEngine.Evaluate(request);

        Assert.Contains(findings, x => x.ValidatorId == "freshness.latest-point");
        Assert.Contains(findings, x => x.ValidatorId == "duplicates.timestamp-conflict");
        Assert.Contains(findings, x => x.ValidatorId == "validity.value-range");
    }

    [Fact]
    public void Evaluate_DetectsRateOfChangeAndFlatLine()
    {
        var request = Request([
            Point("2026-01-01T10:00:00Z", 1),
            Point("2026-01-01T10:15:00Z", 1),
            Point("2026-01-01T10:30:00Z", 1),
            Point("2026-01-01T10:45:00Z", 20)
        ]) with
        {
            MaximumAbsoluteChange = 5,
            FlatLinePointCount = 3
        };

        var findings = QualityValidationEngine.Evaluate(request);

        Assert.Contains(findings, x => x.ValidatorId == "continuity.rate-of-change");
        Assert.Contains(findings, x => x.ValidatorId == "stale.flat-line");
    }

    private static QualityEvaluationRequest Request(List<TimeSeriesPointDto> points) => new(
        new DatasetMetadataDto
        {
            Id = "dataset-1",
            CurveId = "curve-1",
            Source = "energinet",
            DataKind = "actual",
            Category = "price",
            Granularity = "PT15M",
            Unit = "EUR/MWh"
        },
        DateTimeOffset.Parse("2026-01-01T10:00:00Z"),
        DateTimeOffset.Parse("2026-01-01T11:00:00Z"),
        DateTimeOffset.Parse("2026-01-01T11:00:00Z"),
        TimeSpan.FromMinutes(15),
        null,
        null,
        null,
        null,
        null,
        0.000001,
        0,
        points);

    private static TimeSeriesPointDto Point(string timestamp, double? value) => new()
    {
        Timestamp = DateTimeOffset.Parse(timestamp),
        Value = value,
        AsOf = DateTimeOffset.Parse("2026-01-01T12:00:00Z")
    };
}
