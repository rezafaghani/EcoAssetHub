namespace EcoAssetHub.API.Application.SolarPanelCommands;

public class SolarPanelDto
{
    public required string Id { get; set; }
    public decimal Capacity { get; set; }
    public long MeterPointId { get; set; }
    public required string CompassOrientation { get; set; }
}