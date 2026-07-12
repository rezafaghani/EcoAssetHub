namespace EcoAssetHub.Domain.Models;

public class CurveDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DatasetId { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public long MeterPointId { get; set; }
    public decimal Capacity { get; set; }
    public RenewableAssetType Type { get; set; }
}
