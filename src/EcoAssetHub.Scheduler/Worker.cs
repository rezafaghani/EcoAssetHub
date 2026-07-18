using Cronos;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Scheduler.Services;
using Microsoft.Extensions.Options;

namespace EcoAssetHub.Scheduler;

public class Worker(
    IServiceScopeFactory scopeFactory,
    RabbitMqJobPublisher publisher,
    IOptions<SchedulerOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await QueueDueSchedulesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(options.Value.PollingSeconds, 5)), stoppingToken);
        }
    }

    private async Task QueueDueSchedulesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIngestionControlRepository>();
        var now = DateTimeOffset.UtcNow;

        foreach (var schedule in await repository.GetEnabledSchedulesAsync(cancellationToken))
        {
            if (!IsDue(schedule, now))
            {
                continue;
            }

            try
            {
                var message = await repository.TryCreateQueuedJobAsync(schedule, now, cancellationToken);
                if (message is null)
                {
                    continue;
                }

                await publisher.PublishAsync(message, cancellationToken);
                logger.LogInformation("Queued ingestion job {JobId} for schedule {ScheduleId} and curve {CurveId}.",
                    message.JobId,
                    message.ScheduleId,
                    message.CurveId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to queue schedule {ScheduleId}.", schedule.Id);
            }
        }
    }

    private static bool IsDue(IngestionSchedule schedule, DateTimeOffset now)
    {
        var from = schedule.LastQueuedAt ?? schedule.CreatedAt.AddMinutes(-1);
        var expression = CronExpression.Parse(schedule.CronExpression);
        var next = expression.GetNextOccurrence(from, TimeZoneInfo.Utc);
        return next.HasValue && next.Value <= now;
    }
}
