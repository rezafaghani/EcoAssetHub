namespace EcoAssetHub.Domain.Entities;

public class IngestionSchedule
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string CurveId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = [];
    public int LookbackHours { get; set; } = 48;
    public int BatchSize { get; set; } = 500;
    public DateTimeOffset? LastQueuedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
