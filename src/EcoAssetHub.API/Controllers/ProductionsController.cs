using System.Net;
using EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.DailyQueries;
using EcoAssetHub.API.Application.ProductionCommands.SpotPriceQueries.MonthlyQueries;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionsController(IMediator mediator) : ControllerBase
    {
        [HttpGet("{meterPointId:long}/SpotPrices", Name = "SpotPricesDaily")]
        [ProducesResponseType(typeof(List<SpotPriceDailyQueryResult>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> SpotPricesDaily([FromRoute] long meterPointId, [FromQuery] string start,
            [FromQuery] string end)
        {
            var query = new SpotPriceDailyQuery
            {
                End = DateTime.Parse(end),
                MeterPointId = meterPointId,
                Start = DateTime.Parse(start)
            };

            var result = await mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("SpotPrices",Name = "MonthlySpotPrice")]
        [ProducesResponseType(typeof(List<SpotPriceMonthlyQueryResult>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> MonthlySpotPrice([FromQuery] SpotPriceMonthlyQuery query)
        {
            var result = await mediator.Send(query);
            return Ok(result);
        }
    }
}
