using EcoAssetHub.Contracts;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Ingestion.Services;

public class IngestionWriteClient(IngestionWrite.IngestionWriteClient client)
{
    public async Task<DatasetMetadataDto> UpsertDatasetAsync(DatasetMetadataDto metadata, CancellationToken cancellationToken)
    {
        var request = new DatasetMetadataMessage
        {
            Id = metadata.Id,
            Source = metadata.Source,
            Endpoint = metadata.Endpoint,
            Metric = metadata.Metric,
            Unit = metadata.Unit,
            Country = metadata.Country,
            BiddingZone = metadata.BiddingZone,
            Region = metadata.Region,
            Granularity = metadata.Granularity,
            ProductionType = metadata.ProductionType,
            ForecastType = metadata.ForecastType,
            Neighbor = metadata.Neighbor,
            LicenseInfo = metadata.LicenseInfo,
            Deprecated = metadata.Deprecated
        };
        request.RequestParameters.Add(metadata.RequestParameters);

        var response = await client.UpsertDatasetAsync(request, cancellationToken: cancellationToken);
        return new DatasetMetadataDto
        {
            Id = response.Id,
            Source = response.Source,
            Endpoint = response.Endpoint,
            Metric = response.Metric,
            Unit = response.Unit,
            Country = response.Country,
            BiddingZone = response.BiddingZone,
            Region = response.Region,
            Granularity = response.Granularity,
            ProductionType = response.ProductionType,
            ForecastType = response.ForecastType,
            Neighbor = response.Neighbor,
            LicenseInfo = response.LicenseInfo,
            Deprecated = response.Deprecated,
            RequestParameters = response.RequestParameters.ToDictionary(x => x.Key, x => x.Value),
            LastIngestedAt = DateTimeOffset.UtcNow
        };
    }

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
