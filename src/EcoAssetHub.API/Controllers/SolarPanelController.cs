using EcoAssetHub.API.Application.SolarPanelCommands.CreateCommands;
using EcoAssetHub.API.Application.SolarPanelCommands.GetQueries;
using EcoAssetHub.API.Application.SolarPanelCommands.UpdateCommands;
using EcoAssetHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolarPanelsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSolarPanelCommand command)
    {
        await mediator.Send(command);
        return Ok();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var query = new GetSolarPanelByIdQuery(id);
            var solarPanelDto = await mediator.Send(query);
            return Ok(solarPanelDto);
        }
        catch (DomainException ex) // Adjust the exception type as per your implementation
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSolarPanelCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");

        await mediator.Send(command);
        return NoContent();
    }
}