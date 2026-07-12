using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Ingestion.Services;

public class NormalizedDataset
{
    public DatasetMetadataDto Metadata { get; set; } = new();
    public TimeSeriesBatchRequest Batch { get; set; } = new();
}
