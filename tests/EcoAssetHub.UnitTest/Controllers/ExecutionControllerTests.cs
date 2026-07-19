using EcoAssetHub.API.Controllers;
using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.UnitTest.Controllers;

public class ExecutionControllerTests
{
    [Fact]
    public void Plugins_ReturnsValidationPluginsFromGenericCatalog()
    {
        var result = Controller().Plugins(ExecutionCategories.Validation);

        var ok = Assert.IsType<OkObjectResult>(result);
        var plugins = Assert.IsType<List<ExecutionPluginDto>>(ok.Value);
        Assert.Contains(plugins, x => x.Id == "timelens.validation.completeness.missing-timestamps");
    }

    [Fact]
    public async Task UpsertDefinition_RejectsUnavailablePlugin()
    {
        var request = new UpsertExecutionDefinitionRequest(
            null,
            "Definition",
            null,
            true,
            "*/15 * * * *",
            "UTC",
            "now-24h",
            "now",
            null,
            null,
            null,
            [new UpsertExecutionDefinitionTargetRequest("dataset", "dataset-1", null)],
            [new UpsertExecutionDefinitionPluginRequest(null, "timelens.analytics.unknown", null, true, null, null, null)]);

        var result = await Controller().UpsertDefinition(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private static ExecutionController Controller() =>
        new(new ExecutionPluginCatalog(new QualityValidatorCatalog()),
            Mock.Of<IExecutionRepository>(),
            Mock.Of<IDatasetRepository>(),
            Mock.Of<ITimeSeriesRepository>(),
            Mock.Of<IQualityRepository>());
}
