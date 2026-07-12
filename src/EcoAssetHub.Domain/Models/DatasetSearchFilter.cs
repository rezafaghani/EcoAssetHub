namespace EcoAssetHub.Domain.Models;

public class DatasetSearchFilter
{
    public string? Search { get; set; }
    public string? CurveId { get; set; }
    public string? Endpoint { get; set; }
    public string? Metric { get; set; }
    public string? Country { get; set; }
    public string? BiddingZone { get; set; }
    public string? Region { get; set; }
    public string? Granularity { get; set; }
}
