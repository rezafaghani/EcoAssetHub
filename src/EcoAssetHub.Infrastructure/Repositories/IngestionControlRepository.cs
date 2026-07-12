using EcoAssetHub.Domain.Models;
using MongoDB.Bson;

namespace EcoAssetHub.Infrastructure.Repositories;

public class IngestionControlRepository(EcoAssetHubContext context) : IIngestionControlRepository
{
    public async Task<List<IngestionSchedule>> GetEnabledSchedulesAsync(CancellationToken cancellationToken = default)
    {
        return await context.IngestionSchedules
            .Find(x => x.Enabled)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<IngestionSchedule>> GetSchedulesAsync(string? curveId = null, CancellationToken cancellationToken = default)
    {
        var filter = string.IsNullOrWhiteSpace(curveId)
            ? Builders<IngestionSchedule>.Filter.Empty
            : Builders<IngestionSchedule>.Filter.Eq(x => x.CurveId, curveId);

        return await context.IngestionSchedules
            .Find(filter)
            .SortBy(x => x.CurveId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<IngestionJob>> GetJobsAsync(string? scheduleId, string? curveId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<IngestionJob>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(scheduleId))
        {
            filter &= Builders<IngestionJob>.Filter.Eq(x => x.ScheduleId, scheduleId);
        }
        if (!string.IsNullOrWhiteSpace(curveId))
        {
            filter &= Builders<IngestionJob>.Filter.Eq(x => x.CurveId, curveId);
        }

        return await context.IngestionJobs
            .Find(filter)
            .SortByDescending(x => x.QueuedAt)
            .Limit(500)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<IngestionExecution>> GetExecutionsAsync(string? jobId, string? scheduleId, string? curveId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<IngestionExecution>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(jobId))
        {
            filter &= Builders<IngestionExecution>.Filter.Eq(x => x.JobId, jobId);
        }
        if (!string.IsNullOrWhiteSpace(scheduleId))
        {
            filter &= Builders<IngestionExecution>.Filter.Eq(x => x.ScheduleId, scheduleId);
        }
        if (!string.IsNullOrWhiteSpace(curveId))
        {
            filter &= Builders<IngestionExecution>.Filter.Eq(x => x.CurveId, curveId);
        }

        return await context.IngestionExecutions
            .Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .Limit(500)
            .ToListAsync(cancellationToken);
    }

    public async Task EnsureDefaultSchedulesAsync(IEnumerable<IngestionSchedule> schedules, CancellationToken cancellationToken = default)
    {
        foreach (var schedule in schedules)
        {
            var existing = await context.IngestionSchedules
                .Find(x => x.Id == schedule.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is null)
            {
                await context.IngestionSchedules.InsertOneAsync(schedule, cancellationToken: cancellationToken);
                continue;
            }

            if (existing.CronExpression != "* * * * *")
            {
                continue;
            }

            await context.IngestionSchedules.UpdateOneAsync(
                Builders<IngestionSchedule>.Filter.Eq(x => x.Id, schedule.Id),
                Builders<IngestionSchedule>.Update
                    .Set(x => x.Name, schedule.Name)
                    .Set(x => x.CurveId, schedule.CurveId)
                    .Set(x => x.CronExpression, schedule.CronExpression)
                    .Set(x => x.Endpoint, schedule.Endpoint)
                    .Set(x => x.Parameters, schedule.Parameters)
                    .Set(x => x.LookbackHours, schedule.LookbackHours)
                    .Set(x => x.BatchSize, schedule.BatchSize)
                    .Set(x => x.UpdatedAt, DateTimeOffset.UtcNow),
                cancellationToken: cancellationToken);
        }
    }

    public async Task<IngestionJobMessage?> TryCreateQueuedJobAsync(IngestionSchedule schedule, DateTimeOffset queuedAt, CancellationToken cancellationToken = default)
    {
        var scheduleFilter = Builders<IngestionSchedule>.Filter.Eq(x => x.Id, schedule.Id);
        scheduleFilter &= schedule.LastQueuedAt.HasValue
            ? Builders<IngestionSchedule>.Filter.Eq(x => x.LastQueuedAt, schedule.LastQueuedAt.Value)
            : Builders<IngestionSchedule>.Filter.Eq(x => x.LastQueuedAt, null);

        var reserved = await context.IngestionSchedules.UpdateOneAsync(
            scheduleFilter,
            Builders<IngestionSchedule>.Update.Set(x => x.LastQueuedAt, queuedAt).Set(x => x.UpdatedAt, queuedAt),
            cancellationToken: cancellationToken);

        if (reserved.ModifiedCount == 0)
        {
            return null;
        }

        var job = new IngestionJob
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ScheduleId = schedule.Id,
            CurveId = schedule.CurveId,
            Status = IngestionStatuses.Queued,
            QueuedAt = queuedAt
        };
        var execution = new IngestionExecution
        {
            Id = ObjectId.GenerateNewId().ToString(),
            JobId = job.Id,
            ScheduleId = schedule.Id,
            CurveId = schedule.CurveId,
            Status = IngestionStatuses.Queued,
            CreatedAt = queuedAt
        };

        await context.IngestionJobs.InsertOneAsync(job, cancellationToken: cancellationToken);
        await context.IngestionExecutions.InsertOneAsync(execution, cancellationToken: cancellationToken);

        return new IngestionJobMessage
        {
            ScheduleId = schedule.Id,
            JobId = job.Id,
            ExecutionId = execution.Id,
            CurveId = schedule.CurveId,
            Endpoint = schedule.Endpoint,
            Parameters = schedule.Parameters,
            LookbackHours = schedule.LookbackHours,
            BatchSize = schedule.BatchSize
        };
    }

    public async Task MarkJobRunningAsync(string jobId, string executionId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await context.IngestionJobs.UpdateOneAsync(
            x => x.Id == jobId,
            Builders<IngestionJob>.Update.Set(x => x.Status, IngestionStatuses.Running).Set(x => x.StartedAt, now),
            cancellationToken: cancellationToken);
        await context.IngestionExecutions.UpdateOneAsync(
            x => x.Id == executionId,
            Builders<IngestionExecution>.Update.Set(x => x.Status, IngestionStatuses.Running).Set(x => x.StartedAt, now),
            cancellationToken: cancellationToken);
    }

    public async Task MarkJobCompletedAsync(string jobId, string executionId, int inserted, int skipped, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await context.IngestionJobs.UpdateOneAsync(
            x => x.Id == jobId,
            Builders<IngestionJob>.Update.Set(x => x.Status, IngestionStatuses.Completed).Set(x => x.FinishedAt, now),
            cancellationToken: cancellationToken);
        await context.IngestionExecutions.UpdateOneAsync(
            x => x.Id == executionId,
            Builders<IngestionExecution>.Update
                .Set(x => x.Status, IngestionStatuses.Completed)
                .Set(x => x.FinishedAt, now)
                .Set(x => x.Inserted, inserted)
                .Set(x => x.Skipped, skipped),
            cancellationToken: cancellationToken);
    }

    public async Task MarkJobFailedAsync(string jobId, string executionId, string error, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await context.IngestionJobs.UpdateOneAsync(
            x => x.Id == jobId,
            Builders<IngestionJob>.Update
                .Set(x => x.Status, IngestionStatuses.Failed)
                .Set(x => x.FinishedAt, now)
                .Set(x => x.Error, error),
            cancellationToken: cancellationToken);
        await context.IngestionExecutions.UpdateOneAsync(
            x => x.Id == executionId,
            Builders<IngestionExecution>.Update
                .Set(x => x.Status, IngestionStatuses.Failed)
                .Set(x => x.FinishedAt, now)
                .Set(x => x.Error, error),
            cancellationToken: cancellationToken);
    }
}
