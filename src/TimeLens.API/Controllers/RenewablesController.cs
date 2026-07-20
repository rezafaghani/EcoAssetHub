using System.Net;
using TimeLens.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace TimeLens.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RenewablesController(IRenewableAssetRepository repository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<RenewAbleDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetAll()
    {
        var assets = await repository.GetAllAsync();
        return Ok(assets.Select(x => new RenewAbleDto
        {
            Id = x.Id,
            Capacity = x.Capacity,
            MeterPointId = x.MeterPointId,
            Type = x.Type,
            CompassOrientation = x.CompassOrientation,
            RotorDiameter = x.RotorDiameter,
            HubHeight = x.HubHeight
        }).ToList());
    }

    [HttpDelete("{id}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Delete(string id)
    {
        var renewable = await repository.GetAsync(id);
        if (renewable == null)
        {
            throw new DomainException($"Renewable Asset with ID {id} not found.");
        }

        await repository.RemoveAsync(id);
        return Ok();
    }


}

public class RenewAbleDto
{
    public required string Id { get; set; }
    public decimal Capacity { get; set; }
    public long MeterPointId { get; set; }
    public decimal? HubHeight { get; set; }
    public decimal? RotorDiameter { get; set; }
    public string? CompassOrientation { get; set; }
    public RenewableAssetType Type { get; set; }
}
