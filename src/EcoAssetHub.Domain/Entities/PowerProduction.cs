namespace EcoAssetHub.Domain.Entities;

public class PowerProduction
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string MeterPointId { get; set; } = string.Empty;

    public DateTimeOffset ProductionDateTime { get; set; }
    public int Production { get; set; }
    public DateTimeOffset AsOf { get; set; }
    public DateTimeOffset InsertedAt { get; set; }
}
