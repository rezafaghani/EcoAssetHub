using EcoAssetHub.API.Application.SolarPanelCommands.CreateCommands;
using EcoAssetHub.API.Application.SolarPanelCommands;
using EcoAssetHub.API.Application.SolarPanelCommands.UpdateCommands;
using EcoAssetHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SolarPanelsController(ISolarPanelRepository repository) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSolarPanelCommand command)
    {
        await repository.CreateAsync((SolarPanel)command);
        return Ok();
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var solarPanel = await repository.GetAsync(id);
            if (solarPanel == null)
            {
                throw new DomainException($"Solar panel with ID {id} not found.");
            }

            return Ok(new SolarPanelDto
            {
                Capacity = solarPanel.Capacity,
                CompassOrientation = solarPanel.CompassOrientation,
                Id = solarPanel.Id,
                MeterPointId = solarPanel.MeterPointId
            });
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

        var solarPanel = await repository.GetAsync(command.Id);
        if (solarPanel == null)
        {
            throw new DomainException($"Solar panel with ID {command.Id} not found.");
        }

        solarPanel.Capacity = command.Capacity;
        solarPanel.CompassOrientation = command.CompassOrientation;

        await repository.UpdateAsync(solarPanel);
        return NoContent();
    }
}
