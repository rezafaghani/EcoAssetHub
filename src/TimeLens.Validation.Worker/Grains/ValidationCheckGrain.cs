using TimeLens.Domain.Interfaces;
using TimeLens.Domain.Models;
using TimeLens.Domain.Services;
using TimeLens.Validation;
using Orleans;

namespace TimeLens.Validation.Worker.Grains;

public class ValidationCheckGrain(
    IEnumerable<IExecutionPlugin> plugins,
    IDatasetRepository datasetRepository,
    ITimeSeriesRepository timeSeriesRepository) : Grain, IValidationCheckGrain
{
    public async Task<List<ExecutionStepResultDto>> ValidateAsync(ValidationCheckMessage message)
    {
        var plugin = plugins.SingleOrDefault(x =>
            x.Metadata.Id.Equals(message.ValidatorId, StringComparison.OrdinalIgnoreCase)
            || x.Metadata.Id.Equals($"{QualityValidationPlugin.IdPrefix}{message.ValidatorId}", StringComparison.OrdinalIgnoreCase));
        if (plugin is null)
        {
            throw new ArgumentException($"Validation plugin '{message.ValidatorId}' is not available in this worker.");
        }

        var metadata = await datasetRepository.GetAsync(message.TargetId);
        if (metadata is null)
        {
            return [];
        }

        var points = await timeSeriesRepository.GetSeriesAsync(message.TargetId, message.Start, message.End, null, CancellationToken.None, 10000);
        var context = new ExecutionPluginContext(
            message.TargetType,
            message.TargetId,
            message.Start,
            message.End,
            message.Configuration,
            metadata,
            points);
        return await plugin.ExecuteAsync(context);
    }
}
