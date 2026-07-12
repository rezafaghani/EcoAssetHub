using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.Insert.Controllers;

[ApiController]
[Route("api/datasets")]
public class DatasetsController(IDatasetRepository datasetRepository) : ControllerBase
{
    [HttpPost("upsert")]
    [ProducesResponseType(typeof(DatasetMetadataDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Upsert([FromBody] DatasetMetadataDto request, CancellationToken cancellationToken)
    {
        var result = await datasetRepository.UpsertAsync(request, cancellationToken);
        return Ok(result);
    }
}
