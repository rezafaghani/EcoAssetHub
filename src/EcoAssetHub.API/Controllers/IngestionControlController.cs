using System.Text.RegularExpressions;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Models;
using EcoAssetHub.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[ApiController]
[Route("api/ingestion")]
public class IngestionControlController(
    IIngestionControlRepository repository,
    IDatasetRepository datasets,
    RabbitMqJobPublisher publisher) : ControllerBase
{
    [HttpGet("schedules")]
    [ProducesResponseType(typeof(List<IngestionSchedule>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Schedules([FromQuery] string? curveId, CancellationToken cancellationToken)
    {
        return Ok(await repository.GetSchedulesAsync(curveId, cancellationToken));
    }

    [HttpGet("jobs")]
    [ProducesResponseType(typeof(List<IngestionJob>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Jobs(
        [FromQuery] string? scheduleId,
        [FromQuery] string? curveId,
        CancellationToken cancellationToken)
    {
        return Ok(await repository.GetJobsAsync(scheduleId, curveId, cancellationToken));
    }

    [HttpGet("executions")]
    [ProducesResponseType(typeof(List<IngestionExecution>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Executions(
        [FromQuery] string? jobId,
        [FromQuery] string? scheduleId,
        [FromQuery] string? curveId,
        CancellationToken cancellationToken)
    {
        return Ok(await repository.GetExecutionsAsync(jobId, scheduleId, curveId, cancellationToken));
    }

    [HttpGet("curves/{curveId}/schedules")]
    [ProducesResponseType(typeof(List<IngestionSchedule>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CurveSchedules([FromRoute] string curveId, CancellationToken cancellationToken)
    {
        return Ok(await repository.GetSchedulesAsync(curveId, cancellationToken));
    }

    [HttpGet("curves/{curveId}/jobs")]
    [ProducesResponseType(typeof(List<IngestionJob>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CurveJobs([FromRoute] string curveId, CancellationToken cancellationToken)
    {
        return Ok(await repository.GetJobsAsync(null, curveId, cancellationToken));
    }

    [HttpGet("curves/{curveId}/executions")]
    [ProducesResponseType(typeof(List<IngestionExecution>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CurveExecutions([FromRoute] string curveId, CancellationToken cancellationToken)
    {
        return Ok(await repository.GetExecutionsAsync(null, null, curveId, cancellationToken));
    }

    [HttpPut("schedules/{id}")]
    [ProducesResponseType(typeof(IngestionSchedule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSchedule([FromRoute] string id, [FromBody] UpdateIngestionScheduleRequest request, CancellationToken cancellationToken)
    {
        var schedule = await repository.GetScheduleAsync(id, cancellationToken);
        if (schedule is null)
        {
            return NotFound();
        }

        var validation = ValidateScheduleRequest(request);
        if (validation is not null)
        {
            return BadRequest(validation);
        }

        schedule.CronExpression = request.CronExpression.Trim();
        schedule.Enabled = request.Enabled;
        schedule.WindowStartExpression = request.WindowStartExpression.Trim();
        schedule.WindowEndExpression = request.WindowEndExpression.Trim();
        schedule.BatchSize = request.BatchSize;

        return Ok(await repository.UpdateScheduleAsync(schedule, cancellationToken));
    }

    [HttpPost("schedules/{id}/reset")]
    [ProducesResponseType(typeof(IngestionSchedule), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetSchedule([FromRoute] string id, CancellationToken cancellationToken)
    {
        var schedule = await repository.ResetScheduleAsync(id, cancellationToken);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [HttpPost("schedules/{id}/backloads")]
    [ProducesResponseType(typeof(IngestionJobMessage), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBackload([FromRoute] string id, [FromBody] CreateBackloadRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DatasetId))
        {
            return BadRequest("Dataset is required.");
        }

        if (string.IsNullOrWhiteSpace(request.WindowStartExpression) || string.IsNullOrWhiteSpace(request.WindowEndExpression))
        {
            return BadRequest("Start and end expressions are required.");
        }

        if (request.BatchSize < 1)
        {
            return BadRequest("Batch size must be at least 1.");
        }

        if (!IsTimeExpression(request.WindowStartExpression) || !IsTimeExpression(request.WindowEndExpression))
        {
            return BadRequest("Use now, today, today-1, today+1, now-48h, or an ISO timestamp for the period.");
        }

        var schedule = await repository.GetScheduleAsync(id, cancellationToken);
        if (schedule is null)
        {
            return NotFound();
        }

        var metadata = await datasets.GetAsync(request.DatasetId, cancellationToken);
        if (metadata is null || metadata.CurveId != schedule.CurveId)
        {
            return BadRequest("Dataset does not belong to this schedule curve.");
        }

        var message = await repository.CreateBackloadJobAsync(
            schedule,
            metadata.Endpoint,
            WithoutDateRange(metadata.RequestParameters),
            request.WindowStartExpression.Trim(),
            request.WindowEndExpression.Trim(),
            request.BatchSize,
            DateTimeOffset.UtcNow,
            cancellationToken);
        await publisher.PublishAsync(message, cancellationToken);

        return Accepted(message);
    }

    private static string? ValidateScheduleRequest(UpdateIngestionScheduleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CronExpression))
        {
            return "Cron expression is required.";
        }

        if (!LooksLikeFiveFieldCron(request.CronExpression))
        {
            return "Cron expression is invalid.";
        }

        if (string.IsNullOrWhiteSpace(request.WindowStartExpression) || string.IsNullOrWhiteSpace(request.WindowEndExpression))
        {
            return "Window start and end expressions are required.";
        }

        if (!IsTimeExpression(request.WindowStartExpression) || !IsTimeExpression(request.WindowEndExpression))
        {
            return "Use now, today, today-1, today+1, now-48h, or an ISO timestamp for the window.";
        }

        return request.BatchSize < 1 ? "Batch size must be at least 1." : null;
    }

    private static bool IsTimeExpression(string value)
    {
        return DateTimeOffset.TryParse(value, out _)
            || Regex.IsMatch(value.Trim(), "^(now|today)([+-]\\d+)?([hd])?$", RegexOptions.IgnoreCase);
    }

    private static bool LooksLikeFiveFieldCron(string value)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 5;
    }

    private static Dictionary<string, string> WithoutDateRange(Dictionary<string, string> parameters)
    {
        var copy = new Dictionary<string, string>(parameters);
        copy.Remove("start");
        copy.Remove("end");
        return copy;
    }
}

public record UpdateIngestionScheduleRequest(
    string CronExpression,
    bool Enabled,
    string WindowStartExpression,
    string WindowEndExpression,
    int BatchSize);

public record CreateBackloadRequest(
    string DatasetId,
    string WindowStartExpression,
    string WindowEndExpression,
    int BatchSize);
