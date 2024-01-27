namespace EcoAssetHub.Domain.Entities;

public class SolarPanel : RenewableAsset
{
    private string _compassOrientation;
    public string CompassOrientation 
    { 
        get => _compassOrientation;
        set
        {
            // Add validation for compass orientation if needed
            _compassOrientation = value;
        }
    }

    public SolarPanel(decimal capacity, long meterPointId, string compassOrientation)
        : base(RenewableAssetType.SolarPanel, capacity, meterPointId)
    {
        CompassOrientation = compassOrientation;
    }
}