using System.Net;
using EcoAssetHub.API.Application.WindTurbineCommands;
using EcoAssetHub.API.Application.WindTurbineCommands.CreateCommands;
using EcoAssetHub.API.Application.WindTurbineCommands.GetQueries;
using EcoAssetHub.API.Application.WindTurbineCommands.UpdateCommands;
using EcoAssetHub.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace EcoAssetHub.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WindTurbinesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateWindTurbineCommand command)
    {
        await mediator.Send(command);
        return Ok();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WindTurbineDto),(int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetById(string id)
    {
        try
        {
            var query = new GetWindTurbineByIdQuery(id);
            var windTurbineDto = await mediator.Send(query);
            return Ok(windTurbineDto);
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

        await mediator.Send(command);
        return NoContent();
    }
}