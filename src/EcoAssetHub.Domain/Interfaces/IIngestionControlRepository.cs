using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Domain.Interfaces;

public interface IIngestionControlRepository
{
    Task<List<IngestionSchedule>> GetEnabledSchedulesAsync(CancellationToken cancellationToken = default);
    Task<List<IngestionSchedule>> GetSchedulesAsync(string? curveId = null, CancellationToken cancellationToken = default);
    Task<IngestionSchedule?> GetScheduleAsync(string id, CancellationToken cancellationToken = default);
    Task<List<IngestionJob>> GetJobsAsync(string? scheduleId, string? curveId, CancellationToken cancellationToken = default);
    Task<List<IngestionExecution>> GetExecutionsAsync(string? jobId, string? scheduleId, string? curveId, CancellationToken cancellationToken = default);
    Task EnsureDefaultSchedulesAsync(IEnumerable<IngestionSchedule> schedules, CancellationToken cancellationToken = default);
    Task<IngestionSchedule?> UpdateScheduleAsync(IngestionSchedule schedule, CancellationToken cancellationToken = default);
    Task<IngestionSchedule?> ResetScheduleAsync(string id, CancellationToken cancellationToken = default);
    Task<IngestionJobMessage?> TryCreateQueuedJobAsync(IngestionSchedule schedule, DateTimeOffset queuedAt, CancellationToken cancellationToken = default);
    Task<IngestionJobMessage> CreateBackloadJobAsync(IngestionSchedule schedule, string endpoint, Dictionary<string, string> parameters, string windowStartExpression, string windowEndExpression, int batchSize, DateTimeOffset queuedAt, CancellationToken cancellationToken = default);
    Task MarkJobRunningAsync(string jobId, string executionId, CancellationToken cancellationToken = default);
    Task MarkJobCompletedAsync(string jobId, string executionId, int inserted, int skipped, CancellationToken cancellationToken = default);
    Task MarkJobFailedAsync(string jobId, string executionId, string error, CancellationToken cancellationToken = default);
}
