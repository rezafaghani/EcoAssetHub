using EcoAssetHub.API.Controllers;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.UnitTest.Controllers;

public class DatasetsControllerTests
{
    [Fact]
    public async Task Series_RejectsEmptyRange()
    {
        var result = await new DatasetsController(Mock.Of<IDatasetRepository>(), Mock.Of<ITimeSeriesRepository>())
            .Series("dataset-1", "2026-01-01T00:00:00Z", "2026-01-01T00:00:00Z", null, null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SetDeprecated_UpdatesDatasetMetadata()
    {
        var repository = new Mock<IDatasetRepository>();
        var cancellationToken = CancellationToken.None;
        repository.Setup(x => x.SetDeprecatedAsync("dataset-1", true, cancellationToken))
            .ReturnsAsync(new DatasetMetadataDto { Id = "dataset-1", Deprecated = true });

        var result = await new DatasetsController(repository.Object, Mock.Of<ITimeSeriesRepository>())
            .SetDeprecated("dataset-1", new SetDatasetDeprecatedRequest(true), cancellationToken);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.True(Assert.IsType<DatasetMetadataDto>(ok.Value).Deprecated);
    }
}
