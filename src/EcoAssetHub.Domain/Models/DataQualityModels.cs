using System.Text.Json;

namespace EcoAssetHub.Domain.Models;

public static class QualityStatuses
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Critical = "critical";
    public const string Unknown = "unknown";
}

public static class QualityExecutionStatuses
{
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Completed = "completed";
    public const string CompletedWithFindings = "completed_with_findings";
    public const string CompletedWithPartialFailures = "completed_with_partial_failures";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
    public const string TimedOut = "timed_out";
}

public record QualityValidatorTypeDto(
    string Id,
    string Category,
    string DisplayName,
    string Description,
    string TargetType,
    int ConfigurationVersion,
    string DefaultSeverity,
    JsonElement ConfigurationSchema);

public record QualityCurveGroupDto(
    string Id,
    string Name,
    string Description,
    string GroupType,
    bool Enabled,
    JsonElement Rule,
    JsonElement Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record UpsertQualityCurveGroupRequest(
    string? Id,
    string Name,
    string? Description,
    string GroupType,
    bool Enabled,
    JsonElement? Rule,
    JsonElement? Tags);

public record QualityValidationJobDto(
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
    List<QualityValidationJobTargetDto> Targets,
    List<QualityValidationJobCheckDto> Checks,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record QualityValidationJobTargetDto(
    string TargetType,
    string TargetId,
    JsonElement Rule);

public record QualityValidationJobCheckDto(
    string Id,
    string ValidatorId,
    int ValidatorVersion,
    bool Enabled,
    JsonElement Configuration,
    JsonElement Severity,
    int SortOrder);

public record UpsertQualityValidationJobRequest(
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
    List<UpsertQualityValidationJobTargetRequest> Targets,
    List<UpsertQualityValidationJobCheckRequest> Checks);

public record UpsertQualityValidationJobTargetRequest(
    string TargetType,
    string TargetId,
    JsonElement? Rule);

public record UpsertQualityValidationJobCheckRequest(
    string? Id,
    string ValidatorId,
    int? ValidatorVersion,
    bool Enabled,
    JsonElement? Configuration,
    JsonElement? Severity,
    int? SortOrder);

public record QualityFindingDto(
    string Id,
    string ExecutionId,
    string? TargetExecutionId,
    string? ValidatorExecutionId,
    string DatasetId,
    string CurveId,
    string ValidatorId,
    string Category,
    string Severity,
    string QualityStatus,
    string TradingImpact,
    string Title,
    string Message,
    DateTimeOffset? AffectedStart,
    DateTimeOffset? AffectedEnd,
    int? ExpectedCount,
    int? ActualCount,
    int? AffectedCount,
    JsonElement SampleTimestamps,
    JsonElement Details,
    string Fingerprint,
    bool Active,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record QualityStatusDto(
    string DatasetId,
    string CurveId,
    string OverallStatus,
    JsonElement CategoryStatuses,
    string LatestExecutionId,
    DateTimeOffset AsOf);

public record QualitySummaryDto(
    int Healthy,
    int Degraded,
    int Critical,
    int Unknown,
    int ActiveFindings,
    int ActiveCriticalFindings);

public record QualityEvaluationRequest(
    DatasetMetadataDto Metadata,
    DateTimeOffset Start,
    DateTimeOffset End,
    DateTimeOffset Now,
    TimeSpan Granularity,
    TimeSpan? AllowedDelay,
    double? MinimumValue,
    double? MaximumValue,
    double? MaximumAbsoluteChange,
    double? MaximumPercentageChange,
    double NearZeroFloor,
    int FlatLinePointCount,
    List<TimeSeriesPointDto> Points);

public record QualityFindingDraftDto(
    string ValidatorId,
    string Category,
    string Severity,
    string QualityStatus,
    string Title,
    string Message,
    DateTimeOffset? AffectedStart,
    DateTimeOffset? AffectedEnd,
    int? ExpectedCount,
    int? ActualCount,
    int? AffectedCount,
    List<DateTimeOffset> SampleTimestamps);
