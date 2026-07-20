namespace TimeLens.Domain.Models;

public class TimeSeriesBatchRequest
{
    public string DatasetId { get; set; } = string.Empty;
    public long MeterPointId { get; set; }
    public string SourceMetadataVersion { get; set; } = string.Empty;
    public List<TimeSeriesWritePoint> Points { get; set; } = [];
}
