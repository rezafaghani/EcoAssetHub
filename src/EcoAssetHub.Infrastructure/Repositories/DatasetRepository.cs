using EcoAssetHub.Domain.Models;
using MongoDB.Bson;

namespace EcoAssetHub.Infrastructure.Repositories;

public class DatasetRepository(EcoAssetHubContext context) : IDatasetRepository
{
    public async Task<DatasetMetadataDto> UpsertAsync(DatasetMetadataDto metadata, CancellationToken cancellationToken = default)
    {
        metadata.Id = string.IsNullOrWhiteSpace(metadata.Id) ? CreateDatasetId(metadata) : metadata.Id;

        var now = DateTimeOffset.UtcNow;
        if (metadata.FirstObservedAt == default)
        {
            metadata.FirstObservedAt = now;
        }

        metadata.LastIngestedAt = now;

        var entity = ToEntity(metadata);
        await context.EnergyDatasets.ReplaceOneAsync(
            x => x.Id == entity.Id,
            entity,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return metadata;
    }

    public async Task<DatasetMetadataDto?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var entity = await context.EnergyDatasets.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<List<DatasetMetadataDto>> SearchAsync(DatasetSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var builder = Builders<EnergyDataset>.Filter;
        var mongoFilter = builder.Empty;

        if (!string.IsNullOrWhiteSpace(filter.Endpoint))
            mongoFilter &= builder.Eq(x => x.Endpoint, filter.Endpoint);
        if (!string.IsNullOrWhiteSpace(filter.CurveId))
            mongoFilter &= builder.Eq(x => x.CurveId, filter.CurveId);
        if (!string.IsNullOrWhiteSpace(filter.Metric))
            mongoFilter &= builder.Eq(x => x.Metric, filter.Metric);
        if (!string.IsNullOrWhiteSpace(filter.Country))
            mongoFilter &= builder.Eq(x => x.Country, filter.Country);
        if (!string.IsNullOrWhiteSpace(filter.BiddingZone))
            mongoFilter &= builder.Eq(x => x.BiddingZone, filter.BiddingZone);
        if (!string.IsNullOrWhiteSpace(filter.Region))
            mongoFilter &= builder.Eq(x => x.Region, filter.Region);
        if (!string.IsNullOrWhiteSpace(filter.Granularity))
            mongoFilter &= builder.Eq(x => x.Granularity, filter.Granularity);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = new BsonRegularExpression(filter.Search.Trim(), "i");
            mongoFilter &= builder.Regex(x => x.Id, search) |
                           builder.Regex(x => x.CurveId, search) |
                           builder.Regex(x => x.Endpoint, search) |
                           builder.Regex(x => x.Metric, search) |
                           builder.Regex(x => x.Unit, search) |
                           builder.Regex(x => x.Country, search) |
                           builder.Regex(x => x.BiddingZone, search) |
                           builder.Regex(x => x.Region, search) |
                           builder.Regex(x => x.ProductionType, search) |
                           builder.Regex(x => x.ForecastType, search) |
                           builder.Regex(x => x.Neighbor, search);
        }

        var entities = await context.EnergyDatasets
            .Find(mongoFilter)
            .SortByDescending(x => x.LastIngestedAt)
            .Limit(500)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDto).ToList();
    }

    private static string CreateDatasetId(DatasetMetadataDto metadata)
    {
        var parts = new[]
        {
            metadata.Source,
            metadata.Endpoint,
            metadata.Metric,
            metadata.Country,
            metadata.BiddingZone,
            metadata.Region,
            metadata.ProductionType,
            metadata.ForecastType,
            metadata.Neighbor,
            metadata.Granularity
        };

        return string.Join(':', parts
            .Select(x => string.IsNullOrWhiteSpace(x) ? "-" : x.Trim().ToLowerInvariant().Replace(' ', '-')));
    }

    private static EnergyDataset ToEntity(DatasetMetadataDto dto) => new()
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
        Deprecated = dto.Deprecated,
        RequestParameters = dto.RequestParameters,
        FirstObservedAt = dto.FirstObservedAt,
        LastIngestedAt = dto.LastIngestedAt
    };

    private static DatasetMetadataDto ToDto(EnergyDataset entity) => new()
    {
        Id = entity.Id,
        CurveId = entity.CurveId,
        Source = entity.Source,
        Endpoint = entity.Endpoint,
        Metric = entity.Metric,
        Unit = entity.Unit,
        Country = entity.Country,
        BiddingZone = entity.BiddingZone,
        Region = entity.Region,
        Granularity = entity.Granularity,
        ProductionType = entity.ProductionType,
        ForecastType = entity.ForecastType,
        Neighbor = entity.Neighbor,
        LicenseInfo = entity.LicenseInfo,
        Deprecated = entity.Deprecated,
        RequestParameters = entity.RequestParameters,
        FirstObservedAt = entity.FirstObservedAt,
        LastIngestedAt = entity.LastIngestedAt
    };
}
