namespace EcoAssetHub.API.Application.RenewAbleCommands;

public class RenewAbleDto
{
    public required string Id { get; set; }
    public decimal Capacity { get; set; }
    public long MeterPointId { get; set; }
    public decimal? HubHeight { get; set; }
    public decimal? RotorDiameter { get; set; }

    public string? CompassOrientation { get; set; }
    public RenewableAssetType Type { get; set; }
}