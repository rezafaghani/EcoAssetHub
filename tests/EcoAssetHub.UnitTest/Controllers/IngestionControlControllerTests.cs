using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.API.Controllers;
using EcoAssetHub.Domain.Models;
using EcoAssetHub.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EcoAssetHub.UnitTest.Controllers;

public class IngestionControlControllerTests
{
    [Fact]
    public async Task CurveEndpoints_FilterByCurveId()
    {
        var repository = new Mock<IIngestionControlRepository>();
        var cancellationToken = CancellationToken.None;
        var curveId = "dk.public_power";

        repository.Setup(x => x.GetSchedulesAsync(curveId, cancellationToken))
            .ReturnsAsync([new IngestionSchedule { CurveId = curveId }]);
        repository.Setup(x => x.GetJobsAsync(null, curveId, cancellationToken))
            .ReturnsAsync([new IngestionJob { CurveId = curveId }]);
        repository.Setup(x => x.GetExecutionsAsync(null, null, curveId, cancellationToken))
            .ReturnsAsync([new IngestionExecution { CurveId = curveId }]);

        var controller = CreateController(repository);

        Assert.Single(Assert.IsType<OkObjectResult>(await controller.CurveSchedules(curveId, cancellationToken)).Value as List<IngestionSchedule> ?? []);
        Assert.Single(Assert.IsType<OkObjectResult>(await controller.CurveJobs(curveId, cancellationToken)).Value as List<IngestionJob> ?? []);
        Assert.Single(Assert.IsType<OkObjectResult>(await controller.CurveExecutions(curveId, cancellationToken)).Value as List<IngestionExecution> ?? []);

        repository.Verify(x => x.GetSchedulesAsync(curveId, cancellationToken), Times.Once);
        repository.Verify(x => x.GetJobsAsync(null, curveId, cancellationToken), Times.Once);
        repository.Verify(x => x.GetExecutionsAsync(null, null, curveId, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task UpdateSchedule_RejectsInvalidCron()
    {
        var repository = new Mock<IIngestionControlRepository>();
        var cancellationToken = CancellationToken.None;
        repository.Setup(x => x.GetScheduleAsync("schedule-1", cancellationToken))
            .ReturnsAsync(new IngestionSchedule { Id = "schedule-1" });

        var result = await CreateController(repository).UpdateSchedule(
            "schedule-1",
            new UpdateIngestionScheduleRequest("not-a-cron", true, "today-1", "today", 500),
            cancellationToken);

        Assert.IsType<BadRequestObjectResult>(result);
        repository.Verify(x => x.UpdateScheduleAsync(It.IsAny<IngestionSchedule>(), cancellationToken), Times.Never);
    }

    [Fact]
    public async Task UpdateSchedule_SavesWindowAndCron()
    {
        var repository = new Mock<IIngestionControlRepository>();
        var cancellationToken = CancellationToken.None;
        repository.Setup(x => x.GetScheduleAsync("schedule-1", cancellationToken))
            .ReturnsAsync(new IngestionSchedule { Id = "schedule-1" });
        repository.Setup(x => x.UpdateScheduleAsync(It.IsAny<IngestionSchedule>(), cancellationToken))
            .ReturnsAsync((IngestionSchedule schedule, CancellationToken _) => schedule);

        var result = await CreateController(repository).UpdateSchedule(
            "schedule-1",
            new UpdateIngestionScheduleRequest("*/15 * * * *", true, "today-1", "today+1", 250),
            cancellationToken);

        Assert.IsType<OkObjectResult>(result);
        repository.Verify(x => x.UpdateScheduleAsync(
            It.Is<IngestionSchedule>(schedule =>
                schedule.CronExpression == "*/15 * * * *"
                && schedule.WindowStartExpression == "today-1"
                && schedule.WindowEndExpression == "today+1"
                && schedule.BatchSize == 250),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task CreateSchedule_CreatesManualSchedule()
    {
        var repository = new Mock<IIngestionControlRepository>();
        var cancellationToken = CancellationToken.None;
        repository.Setup(x => x.CreateScheduleAsync(It.IsAny<IngestionSchedule>(), cancellationToken))
            .ReturnsAsync((IngestionSchedule schedule, CancellationToken _) => schedule);

        var result = await CreateController(repository).CreateSchedule(
            new CreateIngestionScheduleRequest(
                "Manual forecast",
                "dk.forecast.solar",
                "energy-charts",
                "public_power_forecast",
                new Dictionary<string, string> { ["country"] = "dk" },
                "*/30 * * * *",
                true,
                48,
                "now-48h",
                "now",
                500),
            cancellationToken);

        Assert.IsType<CreatedAtActionResult>(result);
        repository.Verify(x => x.CreateScheduleAsync(
            It.Is<IngestionSchedule>(schedule =>
                schedule.CurveId == "dk.forecast.solar"
                && schedule.Source == "energy-charts"
                && schedule.Endpoint == "public_power_forecast"
                && schedule.Enabled),
            cancellationToken), Times.Once);
    }

    private static IngestionControlController CreateController(Mock<IIngestionControlRepository> repository)
    {
        var datasets = new Mock<IDatasetRepository>();
        var publisher = new RabbitMqJobPublisher(Options.Create(new RabbitMqOptions()));
        return new IngestionControlController(repository.Object, datasets.Object, publisher);
    }
}
