namespace EcoAssetHub.API.Application.SolarPanelCommands.CreateCommands;

public class CreateSolarPanelCommand : IRequest<string>
{
    public decimal Capacity { get; set; }
    public long MeterPointId { get; set; }
    public string CompassOrientation { get; set; }


    public static explicit operator SolarPanel(CreateSolarPanelCommand v)
    {
        return new SolarPanel(v.Capacity, v.MeterPointId, v.CompassOrientation);
    }
}