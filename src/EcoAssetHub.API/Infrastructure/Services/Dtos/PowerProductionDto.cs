namespace EcoAssetHub.API.Infrastructure.Services.Dtos;

public class PowerProductionDto
{
    public long MeterPointId { get; set; }
    public DateTimeOffset ProductionDateTime { get; set; }
    public int Production { get; set; }

    public static explicit operator PowerProduction(PowerProductionDto v)
    {
        return new PowerProduction
        {
            MeterPointId = v.MeterPointId.ToString(),
            Production = v.Production,
            ProductionDateTime = v.ProductionDateTime.DateTime
        };
    }
}