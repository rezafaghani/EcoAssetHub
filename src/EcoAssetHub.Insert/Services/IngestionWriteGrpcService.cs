using EcoAssetHub.Contracts;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace EcoAssetHub.Insert.Services;

public class IngestionWriteGrpcService(
    IDatasetRepository datasetRepository,
    ITimeSeriesRepository timeSeriesRepository) : IngestionWrite.IngestionWriteBase
{
    public override async Task<DatasetMetadataMessage> UpsertDataset(DatasetMetadataMessage request, ServerCallContext context)
    {
        var saved = await datasetRepository.UpsertAsync(ToDto(request), context.CancellationToken);
        return ToMessage(saved);
    }

    public override async Task<TimeSeriesInsertResultMessage> InsertTimeSeriesBatch(TimeSeriesBatchMessage request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.DatasetId) || request.Points.Count == 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "DatasetId and at least one point are required."));
        }

        var result = await timeSeriesRepository.InsertBatchAsync(new TimeSeriesBatchRequest
        {
            DatasetId = request.DatasetId,
            SourceMetadataVersion = request.SourceMetadataVersion,
            Points = request.Points.Select(x => new TimeSeriesWritePoint
            {
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(x.UnixSeconds),
                Value = x.HasValue ? x.Value : null
            }).ToList()
        }, context.CancellationToken);

        return new TimeSeriesInsertResultMessage
        {
            Inserted = result.Inserted,
            Skipped = result.Skipped,
            AsOfUnixSeconds = result.AsOf.ToUnixTimeSeconds()
        };
    }

    private static DatasetMetadataDto ToDto(DatasetMetadataMessage message) => new()
    {
        Id = message.Id,
        CurveId = message.CurveId,
        Source = message.Source,
        Endpoint = message.Endpoint,
        Metric = message.Metric,
        Unit = message.Unit,
        Country = message.Country,
        BiddingZone = message.BiddingZone,
        Region = message.Region,
        Granularity = message.Granularity,
        ProductionType = message.ProductionType,
        ForecastType = message.ForecastType,
        Neighbor = message.Neighbor,
        LicenseInfo = message.LicenseInfo,
        Deprecated = message.Deprecated,
        RequestParameters = message.RequestParameters.ToDictionary(x => x.Key, x => x.Value)
    };

    private static DatasetMetadataMessage ToMessage(DatasetMetadataDto dto)
    {
        var message = new DatasetMetadataMessage
        {
            Id = dto.Id,
            CurveId = dto.CurveId,
            Source = dto.Source,
            Endpoint = dto.Endpoint,
            Metric = dto.Metric,
            Unit = dto.Unit,
            Country = dto.Country,
            BiddingZone = dto.BiddingZone,
            Region = dto.Region,
            Granularity = dto.Granularity,
            ProductionType = dto.ProductionType,
            ForecastType = dto.ForecastType,
            Neighbor = dto.Neighbor,
            LicenseInfo = dto.LicenseInfo,
            Deprecated = dto.Deprecated
        };
        message.RequestParameters.Add(dto.RequestParameters);
        return message;
    }
}
