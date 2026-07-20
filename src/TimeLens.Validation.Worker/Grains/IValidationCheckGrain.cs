using TimeLens.Domain.Models;
using Orleans;

namespace TimeLens.Validation.Worker.Grains;

public interface IValidationCheckGrain : IGrainWithStringKey
{
    Task<List<ExecutionStepResultDto>> ValidateAsync(ValidationCheckMessage message);
}
