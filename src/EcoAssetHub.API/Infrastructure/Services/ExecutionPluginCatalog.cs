using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.API.Infrastructure.Services;

public class ExecutionPluginCatalog(QualityValidatorCatalog qualityValidatorCatalog)
{
    public List<ExecutionPluginDto> List() =>
        qualityValidatorCatalog.List()
            .Select(x => new ExecutionPluginDto(
                $"timelens.validation.{x.Id}",
                x.DisplayName,
                x.Description,
                ExecutionCategories.Validation,
                x.ConfigurationVersion,
                [x.TargetType],
                [],
                [],
                x.ConfigurationSchema,
                x.ConfigurationSchema,
                "quality.findings"))
            .ToList();
}
