namespace EcoAssetHub.Domain.Models;

public class TimeSeriesInsertResult
{
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public DateTimeOffset AsOf { get; set; }
}
