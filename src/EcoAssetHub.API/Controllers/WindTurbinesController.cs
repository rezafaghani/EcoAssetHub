using System.Net;
using EcoAssetHub.API.Application.WindTurbineCommands;
using EcoAssetHub.API.Application.WindTurbineCommands.CreateCommands;
using EcoAssetHub.API.Application.WindTurbineCommands.UpdateCommands;
using EcoAssetHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EcoAssetHub.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WindTurbinesController(IWindTurbineRepository repository) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWindTurbineCommand command)
    {
        await repository.CreateAsync((WindTurbine)command);
        return Ok();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WindTurbineDto),(int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var windTurbine = await repository.GetAsync(id);
            if (windTurbine == null)
            {
                throw new DomainException($"Wind turbine with ID {id} not found.");
            }

            return Ok(new WindTurbineDto
            {
                Id = windTurbine.Id,
                Capacity = windTurbine.Capacity,
                MeterPointId = windTurbine.MeterPointId,
                HubHeight = windTurbine.HubHeight,
                RotorDiameter = windTurbine.RotorDiameter
            });
        }
        catch (DomainException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateWindTurbineCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");

        var windTurbine = await repository.GetAsync(command.Id);
        if (windTurbine == null)
        {
            throw new DomainException($"Wind turbine with ID {command.Id} not found.");
        }

        windTurbine.Capacity = command.Capacity;
        windTurbine.MeterPointId = command.MeterPointId;
        windTurbine.HubHeight = command.HubHeight;
        windTurbine.RotorDiameter = command.RotorDiameter;

        await repository.UpdateAsync(windTurbine);
        return NoContent();
    }
}
