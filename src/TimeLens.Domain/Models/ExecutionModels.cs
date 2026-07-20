using System.Text.Json;

namespace TimeLens.Domain.Models;

public static class ExecutionCategories
{
    public const string Validation = "validation";
    public const string Analytics = "analytics";
    public const string Signals = "signals";
    public const string Forecasting = "forecasting";
    public const string Strategies = "strategies";
    public const string Backtesting = "backtesting";
    public const string Notifications = "notifications";
    public const string Automation = "automation";
}

public record ExecutionPluginDto(
    string Id,
    string Name,
    string Description,
    string Category,
    int Version,
    List<string> SupportedTargets,
    List<string> SupportedProviders,
    List<string> RequiredPermissions,
    JsonElement ConfigurationSchema,
    JsonElement DefaultConfiguration,
    string ResultType);

public record ExecutionDefinitionDto(
    string Id,
    string Name,
    string Description,
    bool Enabled,
    string CronExpression,
    string TimeZone,
    string WindowStartExpression,
    string WindowEndExpression,
    int MaxParallelism,
    int TimeoutSeconds,
    JsonElement Tags,
    List<ExecutionDefinitionTargetDto> Targets,
    List<ExecutionDefinitionPluginDto> Plugins,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastQueuedAt);

public record ExecutionDefinitionTargetDto(
    string TargetType,
    string TargetId,
    JsonElement Rule);

public record ExecutionDefinitionPluginDto(
    string Id,
    string PluginId,
    int PluginVersion,
    bool Enabled,
    JsonElement Configuration,
    JsonElement Severity,
    int SortOrder);

public record UpsertExecutionDefinitionRequest(
    string? Id,
    string Name,
    string? Description,
    bool Enabled,
    string CronExpression,
    string TimeZone,
    string WindowStartExpression,
    string WindowEndExpression,
    int? MaxParallelism,
    int? TimeoutSeconds,
    JsonElement? Tags,
    List<UpsertExecutionDefinitionTargetRequest> Targets,
    List<UpsertExecutionDefinitionPluginRequest> Plugins);

public record UpsertExecutionDefinitionTargetRequest(
    string TargetType,
    string TargetId,
    JsonElement? Rule);

public record UpsertExecutionDefinitionPluginRequest(
    string? Id,
    string PluginId,
    int? PluginVersion,
    bool Enabled,
    JsonElement? Configuration,
    JsonElement? Severity,
    int? SortOrder);

public record RunExecutionDefinitionRequest(string? Start, string? End, string? TriggerType = null);

public record ExecutionRunDto(
    string Id,
    string DefinitionId,
    string TriggerType,
    string Status,
    DateTimeOffset QueuedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    DateTimeOffset? EvaluatedStart,
    DateTimeOffset? EvaluatedEnd,
    int TargetCount,
    int CompletedCount,
    int FindingCount,
    int CriticalCount,
    string Error);

public record ExecutionStepResultDto(
    string PluginId,
    string TargetId,
    string Status,
    string ResultType,
    string Summary,
    JsonElement Metrics,
    JsonElement Payload);

public record RunExecutionDefinitionResult(
    ExecutionRunDto Run,
    List<ExecutionStepResultDto> Results);
