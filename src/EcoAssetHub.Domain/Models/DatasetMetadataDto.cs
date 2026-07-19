namespace EcoAssetHub.Domain.Models;

public class DatasetMetadataDto
{
    public string Id { get; set; } = string.Empty;
    public string CurveId { get; set; } = string.Empty;
    public string Source { get; set; } = "energy-charts";
    public string Endpoint { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string DataKind { get; set; } = "actual";
    public string Category { get; set; } = "unknown";
    public string Unit { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string BiddingZone { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Granularity { get; set; } = string.Empty;
    public string ProductionType { get; set; } = string.Empty;
    public string ForecastType { get; set; } = string.Empty;
    public string Neighbor { get; set; } = string.Empty;
    public string LicenseInfo { get; set; } = string.Empty;
    public bool Deprecated { get; set; }
    public Dictionary<string, string> RequestParameters { get; set; } = [];
    public DateTimeOffset FirstObservedAt { get; set; }
    public DateTimeOffset LastIngestedAt { get; set; }
}
