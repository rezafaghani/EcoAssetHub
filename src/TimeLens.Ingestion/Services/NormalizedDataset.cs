using TimeLens.Domain.Models;

namespace TimeLens.Ingestion.Services;

public class NormalizedDataset
{
    public DatasetMetadataDto Metadata { get; set; } = new();
    public TimeSeriesBatchRequest Batch { get; set; } = new();
}
