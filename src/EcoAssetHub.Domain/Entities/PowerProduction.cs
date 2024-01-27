namespace EcoAssetHub.Domain.Entities;

[BsonDiscriminator(RootClass = true)]
public class PowerProduction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    public string MeterPointId { get; set; }

    public DateTimeOffset ProductionDateTime { get; set; }
    public int Production { get; set; }
}