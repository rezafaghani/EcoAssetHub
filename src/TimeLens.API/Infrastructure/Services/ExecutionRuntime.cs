using System.Text.Json;
using TimeLens.Domain.Models;
using TimeLens.Domain.Services;

namespace TimeLens.API.Infrastructure.Services;

public class ExecutionRuntime(
    ExecutionPluginRegistry registry,
    IDatasetRepository datasetRepository,
    ITimeSeriesRepository timeSeriesRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<ExecutionStepResultDto>> ExecuteAsync(
        ExecutionDefinitionDto definition,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken = default)
    {
        var plugins = definition.Plugins
            .Where(x => x.Enabled)
            .OrderBy(x => x.SortOrder)
            .ToList();
        var results = new List<ExecutionStepResultDto>();

        foreach (var target in definition.Targets)
        {
            var targetContext = await ResolveTargetAsync(target, start, end, cancellationToken);
            foreach (var definitionPlugin in plugins)
            {
                var plugin = registry.Resolve(definitionPlugin.PluginId)
                    ?? throw new ArgumentException($"Plugin '{definitionPlugin.PluginId}' is not available in this environment.");
                if (!plugin.Metadata.SupportedTargets.Contains(target.TargetType, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var context = targetContext with { Configuration = definitionPlugin.Configuration };
                results.AddRange(await plugin.ExecuteAsync(context, cancellationToken));
            }
        }

        return results;
    }

    private async Task<ExecutionPluginContext> ResolveTargetAsync(
        ExecutionDefinitionTargetDto target,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        if (!target.TargetType.Equals("dataset", StringComparison.OrdinalIgnoreCase))
        {
            return new ExecutionPluginContext(target.TargetType, target.TargetId, start, end, EmptyJson(), null, []);
        }

        var metadata = await datasetRepository.GetAsync(target.TargetId, cancellationToken)
            ?? throw new ArgumentException($"Dataset '{target.TargetId}' was not found.");
        var points = await timeSeriesRepository.GetSeriesAsync(target.TargetId, start, end, null, cancellationToken, 10000);
        return new ExecutionPluginContext(target.TargetType, target.TargetId, start, end, EmptyJson(), metadata, points);
    }

    private static JsonElement EmptyJson() => JsonSerializer.SerializeToElement(new { }, JsonOptions);
}
