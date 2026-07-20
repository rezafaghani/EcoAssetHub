using System.Text.Json;
using TimeLens.Domain.Models;

namespace TimeLens.Domain.Services;

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
