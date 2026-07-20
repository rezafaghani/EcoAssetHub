namespace TimeLens.Domain.Models;

public class TimeSeriesWritePoint
{
    public DateTimeOffset Timestamp { get; set; }
    public double? Value { get; set; }
}
