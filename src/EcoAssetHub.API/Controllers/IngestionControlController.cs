using EcoAssetHub.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[ApiController]
[Route("api/ingestion")]
public class IngestionControlController(IIngestionControlRepository repository) : ControllerBase
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
}
