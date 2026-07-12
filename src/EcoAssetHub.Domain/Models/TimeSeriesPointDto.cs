namespace EcoAssetHub.Domain.Models;

public class TimeSeriesPointDto
{
    public DateTimeOffset Timestamp { get; set; }
    public double? Value { get; set; }
    public DateTimeOffset AsOf { get; set; }
}
