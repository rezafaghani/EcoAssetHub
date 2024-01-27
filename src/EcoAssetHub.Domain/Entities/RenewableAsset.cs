namespace EcoAssetHub.Domain.Entities;

[BsonDiscriminator(RootClass = true)]
[BsonKnownTypes(typeof(SolarPanel), typeof(WindTurbine))]
public class RenewableAsset
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; private set; }

    public RenewableAssetType Type { get; private set; } // WindTurbine, SolarPanel, etc.

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

    public List<ObjectId> PowerProductionIds { get; set; }

    // Constructor for immutability
    public RenewableAsset(RenewableAssetType type, decimal capacity, long meterPointId)
    {
        Type = type;
        Capacity = capacity;
        MeterPointId = meterPointId;
    }
}