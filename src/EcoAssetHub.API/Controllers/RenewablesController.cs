using System.Net;
using EcoAssetHub.API.Application.RenewAbleCommands;
using EcoAssetHub.API.Application.RenewAbleCommands.DeleteCommands;
using EcoAssetHub.API.Application.RenewAbleCommands.GetQueries;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RenewablesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<RenewAbleDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> GetAll()
    {
        var query = new GetAllQuery();
        var result = await mediator.Send(query);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> Delete(string id)
    {
        await mediator.Send(new DeleteRenewAbleCommand(id));
        return Ok();
    }


}