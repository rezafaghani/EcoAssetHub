using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.Insert.Controllers;

[ApiController]
[Route("api/time-series")]
public class TimeSeriesController(ITimeSeriesRepository timeSeriesRepository) : ControllerBase
{
    [HttpPost("batches")]
    [ProducesResponseType(typeof(TimeSeriesInsertResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InsertBatch([FromBody] TimeSeriesBatchRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DatasetId) || request.Points.Count == 0)
        {
            return BadRequest("DatasetId and at least one point are required.");
        }

        var result = await timeSeriesRepository.InsertBatchAsync(request, cancellationToken);
        return Ok(result);
    }
}
