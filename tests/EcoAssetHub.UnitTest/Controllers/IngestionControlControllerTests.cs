using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Query.Controllers;
using Microsoft.AspNetCore.Mvc;

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

        var controller = new IngestionControlController(repository.Object);

        Assert.Single(Assert.IsType<OkObjectResult>(await controller.CurveSchedules(curveId, cancellationToken)).Value as List<IngestionSchedule> ?? []);
        Assert.Single(Assert.IsType<OkObjectResult>(await controller.CurveJobs(curveId, cancellationToken)).Value as List<IngestionJob> ?? []);
        Assert.Single(Assert.IsType<OkObjectResult>(await controller.CurveExecutions(curveId, cancellationToken)).Value as List<IngestionExecution> ?? []);

        repository.Verify(x => x.GetSchedulesAsync(curveId, cancellationToken), Times.Once);
        repository.Verify(x => x.GetJobsAsync(null, curveId, cancellationToken), Times.Once);
        repository.Verify(x => x.GetExecutionsAsync(null, null, curveId, cancellationToken), Times.Once);
    }
}
