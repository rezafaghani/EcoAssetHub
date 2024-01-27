namespace EcoAssetHub.Domain.Entities;

public class WindTurbine : RenewableAsset
{
    public decimal HubHeight { get; set; }
    public decimal RotorDiameter { get; set; }

    public WindTurbine(decimal capacity, long meterPointId, decimal hubHeight, decimal rotorDiameter)
        : base(RenewableAssetType.WindTurbine, capacity, meterPointId)
    {
        HubHeight = hubHeight;
        RotorDiameter = rotorDiameter;
    }
}