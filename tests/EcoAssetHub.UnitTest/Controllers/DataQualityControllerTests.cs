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
        var result = new DataQualityController(new QualityValidatorCatalog(), Mock.Of<IQualityRepository>())
            .ValidationTypes();

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

        var result = await new DataQualityController(new QualityValidatorCatalog(), Mock.Of<IQualityRepository>())
            .UpsertJob(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
