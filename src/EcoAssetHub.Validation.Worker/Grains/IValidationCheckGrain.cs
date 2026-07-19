using EcoAssetHub.Domain.Models;
using Orleans;

namespace EcoAssetHub.Validation.Worker.Grains;

public interface IValidationCheckGrain : IGrainWithStringKey
{
    Task<List<ExecutionStepResultDto>> ValidateAsync(ValidationCheckMessage message);
}
