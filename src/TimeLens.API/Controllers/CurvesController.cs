using TimeLens.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace TimeLens.API.Controllers;

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
        [FromQuery] string start,
        [FromQuery] string end,
        [FromQuery] string? asOf,
        [FromQuery] string? timeZone,
        CancellationToken cancellationToken)
    {
        if (meterPointId <= 0
            || !DateTimeExpression.TryResolve(start, out var startTime, timeZone)
            || !DateTimeExpression.TryResolve(end, out var endTime, timeZone)
            || startTime > endTime)
        {
            return BadRequest("A valid meterPointId and date range are required.");
        }

        DateTimeOffset? versionTime = null;
        if (!string.IsNullOrWhiteSpace(asOf))
        {
            if (!DateTimeExpression.TryResolve(asOf, out var parsedAsOf, timeZone))
            {
                return BadRequest("A valid asOf version time is required.");
            }

            versionTime = parsedAsOf;
        }

        var points = await productionRepository.GetSeriesAsync(meterPointId, startTime, endTime, versionTime, cancellationToken);
        return Ok(points);
    }
}
