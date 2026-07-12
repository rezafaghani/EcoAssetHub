using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.Query.Controllers;

[ApiController]
[Route("api/ingestion")]
public class IngestionControlController(IIngestionControlRepository repository) : ControllerBase
{
    [HttpGet("schedules")]
    [ProducesResponseType(typeof(List<IngestionSchedule>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Schedules(CancellationToken cancellationToken)
    {
        return Ok(await repository.GetSchedulesAsync(cancellationToken));
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
        CancellationToken cancellationToken)
    {
        return Ok(await repository.GetExecutionsAsync(jobId, scheduleId, cancellationToken));
    }
}
