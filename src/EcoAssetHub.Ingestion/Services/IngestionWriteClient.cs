using EcoAssetHub.Contracts;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Ingestion.Services;

public class IngestionWriteClient(IngestionWrite.IngestionWriteClient client)
{
    public async Task<TimeSeriesInsertResult> InsertBatchAsync(TimeSeriesBatchRequest batch, CancellationToken cancellationToken)
    {
        var request = new TimeSeriesBatchMessage
        {
            DatasetId = batch.DatasetId,
            SourceMetadataVersion = batch.SourceMetadataVersion
        };
        request.Points.AddRange(batch.Points.Select(x => new TimeSeriesWritePointMessage
        {
            UnixSeconds = x.Timestamp.ToUnixTimeSeconds(),
            HasValue = x.Value.HasValue,
            Value = x.Value ?? 0
        }));

        var response = await client.InsertTimeSeriesBatchAsync(request, cancellationToken: cancellationToken);
        return new TimeSeriesInsertResult
        {
            Inserted = response.Inserted,
            Skipped = response.Skipped,
            AsOf = DateTimeOffset.FromUnixTimeSeconds(response.AsOfUnixSeconds)
        };
    }
}
