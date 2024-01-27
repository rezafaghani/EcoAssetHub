namespace EcoAssetHub.API.Application.WindTurbineCommands.CreateCommands;

public class CreateWindTurbineCommand : IRequest<string>
{
    public decimal Capacity { get; set; }
    public long MeterPointId { get; set; }
    public decimal HubHeight { get; set; }
    public decimal RotorDiameter { get; set; }

    public static explicit operator WindTurbine(CreateWindTurbineCommand v)
    {
        return new WindTurbine(v.Capacity, v.MeterPointId, v.HubHeight, v.RotorDiameter);
    }
}