namespace EcoAssetHub.API.Application.SolarPanelCommands.UpdateCommands;

public class UpdateSolarPanelCommand : IRequest
{
    public required string Id { get; set; } // ID of the solar panel to be updated
    public decimal Capacity { get; set; }
    public required string CompassOrientation { get; set; }

}