using System.Net;
using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace EcoAssetHub.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductionsController(ICacheService cacheService, IProductionRepository productionRepository) : ControllerBase
{
    [HttpGet("{meterPointId:long}/SpotPrices", Name = "SpotPricesDaily")]
    [ProducesResponseType(typeof(List<SpotPriceDailyQueryResult>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> SpotPricesDaily([FromRoute] long meterPointId, [FromQuery] string start,
        [FromQuery] string end)
    {
        var productionResult = await productionRepository.SpotPricesDaily(new PowerProductionFilter
        {
            EndDateTime = DateTime.Parse(end),
            MeterPointId = meterPointId,
            StartDateTime = DateTime.Parse(start)
        });

        foreach (var item in productionResult)
        {
            var priceOfDay = cacheService.RetrieveByDateTime(item.Start.Date);
            if (priceOfDay != null)
                item.Production *= priceOfDay.Price;
        }

        return Ok(productionResult.Select(x => new SpotPriceDailyQueryResult
        {
            Start = x.Start,
            End = x.End,
            Value = $"{x.Production:F2} DKK/kWh"
        }).ToList());
    }

    [HttpGet("SpotPrices",Name = "MonthlySpotPrice")]
    [ProducesResponseType(typeof(List<SpotPriceMonthlyQueryResult>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> MonthlySpotPrice([FromQuery] SpotPriceMonthlyQuery query)
    {
        var productionResult = await productionRepository.SpotPriceMonthly(new PowerProductionFilter
        {
            StartDateTime = query.Start,
            EndDateTime = query.End,
        });

        var priceOfMonth = cacheService.RetrieveDateForMonth(query.Start, query.End);
        foreach (var data in productionResult)
        {
            if (priceOfMonth.TryGetValue(data.Month, out var multiplier))
            {
                data.Production *= multiplier;
            }
        }

        return Ok(productionResult.Select(x => new SpotPriceMonthlyQueryResult
        {
            Month = x.Month,
            MeterPointId = x.MeterPointId,
            Production = $"{x.Production:F2} DKK/kWh"
        }).ToList());
    }
}

public class SpotPriceDailyQueryResult
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public required string Value { get; set; }
}

public class SpotPriceMonthlyQuery
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public class SpotPriceMonthlyQueryResult
{
    public required string MeterPointId { get; set; }
    public int Month { get; set; }
    public required string Production { get; set; }
}
