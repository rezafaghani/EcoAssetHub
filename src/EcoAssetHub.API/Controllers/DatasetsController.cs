using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[ApiController]
[Route("api/datasets")]
public class DatasetsController(
    IDatasetRepository datasetRepository,
    ITimeSeriesRepository timeSeriesRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<DatasetMetadataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? curveId,
        [FromQuery] string? endpoint,
        [FromQuery] string? metric,
        [FromQuery] string? country,
        [FromQuery] string? biddingZone,
        [FromQuery] string? region,
        [FromQuery] string? granularity,
        CancellationToken cancellationToken)
    {
        var datasets = await datasetRepository.SearchAsync(new DatasetSearchFilter
        {
            Search = search,
            CurveId = curveId,
            Endpoint = endpoint,
            Metric = metric,
            Country = country,
            BiddingZone = biddingZone,
            Region = region,
            Granularity = granularity
        }, cancellationToken);

        return Ok(datasets);
    }

    [HttpGet("{id}/metadata")]
    [ProducesResponseType(typeof(DatasetMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Metadata([FromRoute] string id, CancellationToken cancellationToken)
    {
        var dataset = await datasetRepository.GetAsync(id, cancellationToken);
        return dataset is null ? NotFound() : Ok(dataset);
    }

    [HttpGet("{id}/series")]
    [ProducesResponseType(typeof(List<TimeSeriesPointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Series(
        [FromRoute] string id,
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end,
        [FromQuery] DateTimeOffset? asOf,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || start > end)
        {
            return BadRequest("A valid dataset id and date range are required.");
        }

        var points = await timeSeriesRepository.GetSeriesAsync(id, start, end, asOf, cancellationToken);
        return Ok(points);
    }
}
