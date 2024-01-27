namespace EcoAssetHub.API.Application.WindTurbineCommands;

public class WindTurbineDto
{
    public required string Id { get; set; }
    public decimal Capacity { get; set; }
    public long MeterPointId { get; set; }
    public decimal HubHeight { get; set; }
    public decimal RotorDiameter { get; set; }
}