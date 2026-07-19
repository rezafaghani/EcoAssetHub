using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Domain.Interfaces;

public interface IQualityRepository
{
    Task<List<QualityCurveGroupDto>> GetCurveGroupsAsync(CancellationToken cancellationToken = default);
    Task<List<QualityValidatorTypeDto>> GetValidatorTypesAsync(ValidationPluginUsage usage, CancellationToken cancellationToken = default);
    Task UpsertValidatorTypesAsync(IEnumerable<RegisteredValidationPluginDto> validators, CancellationToken cancellationToken = default);
    Task<QualityCurveGroupDto> UpsertCurveGroupAsync(UpsertQualityCurveGroupRequest request, CancellationToken cancellationToken = default);
    Task<QualityCurveGroupDto?> SetCurveGroupEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default);
    Task<List<QualityCurveGroupMemberDto>> GetCurveGroupMembersAsync(string groupId, CancellationToken cancellationToken = default);
    Task<List<QualityCurveGroupMemberDto>> ReplaceCurveGroupMembersAsync(string groupId, ReplaceQualityCurveGroupMembersRequest request, CancellationToken cancellationToken = default);
    Task<List<QualityValidationJobDto>> GetJobsAsync(CancellationToken cancellationToken = default);
    Task<List<QualityValidationJobDto>> GetEnabledJobsAsync(CancellationToken cancellationToken = default);
    Task<QualityValidationJobDto?> GetJobAsync(string id, CancellationToken cancellationToken = default);
    Task<QualityValidationJobDto> UpsertJobAsync(UpsertQualityValidationJobRequest request, CancellationToken cancellationToken = default);
    Task<QualityValidationJobDto?> SetJobEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default);
    Task MarkJobQueuedAsync(string id, DateTimeOffset queuedAt, CancellationToken cancellationToken = default);
    Task<List<QualityFindingDto>> GetFindingsAsync(string? datasetId, string? curveId, bool activeOnly, CancellationToken cancellationToken = default);
    Task<QualityStatusDto?> GetStatusAsync(string? datasetId, string? curveId, CancellationToken cancellationToken = default);
    Task<QualitySummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<string> SaveManualEvaluationAsync(ManualQualityEvaluationResult result, CancellationToken cancellationToken = default);
    Task<string> SaveJobEvaluationAsync(string executionId, string jobId, string triggerType, ManualQualityEvaluationResult result, CancellationToken cancellationToken = default);
}
