namespace TimeLens.Domain.Entities;

public class RenewableAsset
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public RenewableAssetType Type { get; private set; } // WindTurbine, SolarPanel, etc.
    public string Name { get; set; } = string.Empty;

    private decimal _capacity;

    public decimal Capacity
    {
        get => _capacity;
        set
        {
            if (value < 0) throw new ArgumentException("Capacity must be a positive value.");
            _capacity = value;
        }
    }

    private long _meterPointId;

    public long MeterPointId
    {
        get => _meterPointId;
        set
        {
            if (value <= 0) throw new ArgumentException("MeterPointId cannot be less tha or equal to zero.");
            _meterPointId = value;
        }
    }

    // Constructor for immutability
    public RenewableAsset(RenewableAssetType type, decimal capacity, long meterPointId)
    {
        Type = type;
        Capacity = capacity;
        MeterPointId = meterPointId;
        Name = meterPointId.ToString();
    }
}
