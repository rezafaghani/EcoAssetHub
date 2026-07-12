namespace EcoAssetHub.Domain.Entities;

public class EnergyTimeSeriesPoint
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    public string DatasetId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public double? Value { get; set; }
    public DateTimeOffset AsOf { get; set; }
    public DateTimeOffset InsertedAt { get; set; }
    public string SourceMetadataVersion { get; set; } = string.Empty;
}
