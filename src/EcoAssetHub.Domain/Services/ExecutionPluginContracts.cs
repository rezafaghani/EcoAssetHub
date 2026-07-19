using System.Text.Json;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Domain.Services;

public interface IExecutionPlugin
{
    ExecutionPluginDto Metadata { get; }
    Task<List<ExecutionStepResultDto>> ExecuteAsync(ExecutionPluginContext context, CancellationToken cancellationToken = default);
}

public record ExecutionPluginContext(
    string TargetType,
    string TargetId,
    DateTimeOffset Start,
    DateTimeOffset End,
    JsonElement Configuration,
    DatasetMetadataDto? Dataset,
    List<TimeSeriesPointDto> Points);
