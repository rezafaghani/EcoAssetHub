using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[ApiController]
[Route("api/curves")]
public class CurvesController(
    IRenewableAssetRepository renewableAssetRepository,
    IProductionRepository productionRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<CurveDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var curves = await renewableAssetRepository.SearchCurvesAsync(search, cancellationToken);
        return Ok(curves);
    }

    [HttpGet("{meterPointId:long}/series")]
    [ProducesResponseType(typeof(List<TimeSeriesPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Series(
        [FromRoute] long meterPointId,
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end,
        [FromQuery] DateTimeOffset? asOf,
        CancellationToken cancellationToken)
    {
        if (meterPointId <= 0 || start > end)
        {
            return BadRequest("A valid meterPointId and date range are required.");
        }

        var points = await productionRepository.GetSeriesAsync(meterPointId, start, end, asOf, cancellationToken);
        return Ok(points);
    }
}
