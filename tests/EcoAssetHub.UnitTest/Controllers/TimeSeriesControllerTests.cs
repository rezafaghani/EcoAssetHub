using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using EcoAssetHub.Insert.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.UnitTest.Controllers;

public class TimeSeriesControllerTests
{
    [Fact]
    public async Task InsertBatch_EmptyRequest_ReturnsBadRequest()
    {
        var repository = new Mock<ITimeSeriesRepository>();
        var controller = new TimeSeriesController(repository.Object);

        var result = await controller.InsertBatch(new TimeSeriesBatchRequest(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        repository.Verify(x => x.InsertBatchAsync(It.IsAny<TimeSeriesBatchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InsertBatch_ValidRequest_WritesTimeSeries()
    {
        var request = new TimeSeriesBatchRequest
        {
            DatasetId = "dataset-1",
            Points = [new TimeSeriesWritePoint { Timestamp = DateTimeOffset.UtcNow, Value = 42 }]
        };
        var repository = new Mock<ITimeSeriesRepository>();
        repository.Setup(x => x.InsertBatchAsync(request, CancellationToken.None))
            .ReturnsAsync(new TimeSeriesInsertResult { Inserted = 1 });
        var controller = new TimeSeriesController(repository.Object);

        var result = await controller.InsertBatch(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, Assert.IsType<TimeSeriesInsertResult>(ok.Value).Inserted);
        repository.Verify(x => x.InsertBatchAsync(request, CancellationToken.None), Times.Once);
    }
}
