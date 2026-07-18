using EcoAssetHub.Contracts;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using Grpc.Core;

namespace EcoAssetHub.Insert.Services;

public class IngestionWriteGrpcService(ITimeSeriesRepository timeSeriesRepository) : IngestionWrite.IngestionWriteBase
{
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
}
