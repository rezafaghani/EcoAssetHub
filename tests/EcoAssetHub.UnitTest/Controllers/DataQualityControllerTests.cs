using EcoAssetHub.API.Controllers;
using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.UnitTest.Controllers;

public class DataQualityControllerTests
{
    [Fact]
    public void ValidationTypes_ReturnsPhaseOneValidators()
    {
        var result = Controller().ValidationTypes();

        var ok = Assert.IsType<OkObjectResult>(result);
        var validators = Assert.IsType<List<QualityValidatorTypeDto>>(ok.Value);
        Assert.Contains(validators, x => x.Id == "completeness.missing-timestamps");
        Assert.Contains(validators, x => x.Id == "freshness.latest-point");
    }

    [Fact]
    public async Task UpsertJob_RejectsUnknownValidator()
    {
        var request = new UpsertQualityValidationJobRequest(
            null,
            "Job",
            null,
            true,
            "*/15 * * * *",
            "UTC",
            "now-24h",
            "now",
            null,
            null,
            null,
            [new UpsertQualityValidationJobTargetRequest("dataset", "dataset-1", null)],
            [new UpsertQualityValidationJobCheckRequest(null, "unknown.validator", null, true, null, null, null)]);

        var result = await Controller().UpsertJob(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Evaluate_ReturnsQualityFindingsForCleanSeriesData()
    {
        var datasetRepository = new Mock<IDatasetRepository>();
        datasetRepository.Setup(x => x.GetAsync("dataset-1", CancellationToken.None))
            .ReturnsAsync(new DatasetMetadataDto
            {
                Id = "dataset-1",
                CurveId = "curve-1",
                Source = "energinet",
                DataKind = "actual",
                Category = "price",
                Granularity = "PT15M",
                Unit = "EUR/MWh"
            });
        var timeSeriesRepository = new Mock<ITimeSeriesRepository>();
        timeSeriesRepository.Setup(x => x.GetSeriesAsync(
                "dataset-1",
                DateTimeOffset.Parse("2026-01-01T10:00:00Z"),
                DateTimeOffset.Parse("2026-01-01T11:00:00Z"),
                null,
                CancellationToken.None,
                10000))
            .ReturnsAsync([
                new TimeSeriesPointDto { Timestamp = DateTimeOffset.Parse("2026-01-01T10:00:00Z"), Value = 1 },
                new TimeSeriesPointDto { Timestamp = DateTimeOffset.Parse("2026-01-01T10:30:00Z"), Value = 3 },
                new TimeSeriesPointDto { Timestamp = DateTimeOffset.Parse("2026-01-01T10:45:00Z"), Value = 4 }
            ]);
        var qualityRepository = new Mock<IQualityRepository>();
        qualityRepository.Setup(x => x.SaveManualEvaluationAsync(It.IsAny<ManualQualityEvaluationResult>(), CancellationToken.None))
            .ReturnsAsync("execution-1");

        var result = await Controller(datasetRepository.Object, timeSeriesRepository.Object, qualityRepository.Object)
            .Evaluate(new ManualQualityEvaluationRequest(
                "dataset-1",
                "2026-01-01T10:00:00Z",
                "2026-01-01T11:00:00Z",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var evaluation = Assert.IsType<ManualQualityEvaluationResult>(ok.Value);
        Assert.Equal(QualityStatuses.Degraded, evaluation.OverallStatus);
        Assert.Equal("execution-1", evaluation.ExecutionId);
        Assert.Contains(evaluation.Findings, x => x.ValidatorId == "completeness.missing-timestamps");
        qualityRepository.Verify(x => x.SaveManualEvaluationAsync(It.IsAny<ManualQualityEvaluationResult>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task RunJob_EvaluatesDirectDatasetTargets()
    {
        var qualityRepository = new Mock<IQualityRepository>();
        qualityRepository.Setup(x => x.GetJobAsync("job-1", CancellationToken.None))
            .ReturnsAsync(new QualityValidationJobDto(
                "job-1",
                "Job",
                "",
                true,
                "*/15 * * * *",
                "UTC",
                "2026-01-01T10:00:00Z",
                "2026-01-01T11:00:00Z",
                4,
                300,
                default,
                [new QualityValidationJobTargetDto("dataset", "dataset-1", default)],
                [new QualityValidationJobCheckDto("check-1", "completeness.missing-timestamps", 1, true, default, default, 0)],
                DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                null));
        qualityRepository.Setup(x => x.SaveManualEvaluationAsync(It.IsAny<ManualQualityEvaluationResult>(), CancellationToken.None))
            .ReturnsAsync("execution-1");
        var datasetRepository = new Mock<IDatasetRepository>();
        datasetRepository.Setup(x => x.GetAsync("dataset-1", CancellationToken.None))
            .ReturnsAsync(new DatasetMetadataDto
            {
                Id = "dataset-1",
                CurveId = "curve-1",
                Source = "energinet",
                DataKind = "actual",
                Category = "price",
                Granularity = "PT15M",
                Unit = "EUR/MWh"
            });
        var timeSeriesRepository = new Mock<ITimeSeriesRepository>();
        timeSeriesRepository.Setup(x => x.GetSeriesAsync(
                "dataset-1",
                DateTimeOffset.Parse("2026-01-01T10:00:00Z"),
                DateTimeOffset.Parse("2026-01-01T11:00:00Z"),
                null,
                CancellationToken.None,
                10000))
            .ReturnsAsync([new TimeSeriesPointDto { Timestamp = DateTimeOffset.Parse("2026-01-01T10:00:00Z"), Value = 1 }]);

        var result = await Controller(datasetRepository.Object, timeSeriesRepository.Object, qualityRepository.Object)
            .RunJob("job-1", new RunQualityJobRequest(null, null), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var run = Assert.IsType<RunQualityJobResult>(ok.Value);
        Assert.Equal(1, run.TargetCount);
        Assert.Equal(1, run.CompletedCount);
        Assert.True(run.FindingCount > 0);
    }

    private static DataQualityController Controller(
        IDatasetRepository? datasetRepository = null,
        ITimeSeriesRepository? timeSeriesRepository = null,
        IQualityRepository? qualityRepository = null) =>
        new(new QualityValidatorCatalog(),
            qualityRepository ?? Mock.Of<IQualityRepository>(),
            datasetRepository ?? Mock.Of<IDatasetRepository>(),
            timeSeriesRepository ?? Mock.Of<ITimeSeriesRepository>());
}
