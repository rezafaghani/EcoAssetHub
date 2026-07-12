using EcoAssetHub.Domain.Models;
using MongoDB.Bson;
using System.Data;

namespace EcoAssetHub.Infrastructure.Repositories;

public class RenewableAssetRepository(EcoAssetHubContext context) : IRenewableAssetRepository
{
    public async Task<List<RenewableAssetDto>> GetAllAsync()
    {
        var resultList = new List<RenewableAssetDto>();
        var documents= await context.RenewableAssets.Find(_ => true).ToListAsync();

        foreach (var doc in documents)
        {
            if (doc is WindTurbine windTurbine)
            {
                resultList.Add(new RenewableAssetDto
                {
                    Id = windTurbine.Id,
                    HubHeight= windTurbine.HubHeight,
                    RotorDiameter=windTurbine.RotorDiameter,
                    MeterPointId = windTurbine.MeterPointId,
                    Capacity = windTurbine.Capacity,
                    Type = RenewableAssetType.WindTurbine
                });
            }
            else if (doc is SolarPanel solarPanel)
            {
                resultList.Add(new RenewableAssetDto
                {
                    Id = solarPanel.Id,
                    MeterPointId = solarPanel.MeterPointId,
                    Capacity = solarPanel.Capacity,
                    CompassOrientation = solarPanel.CompassOrientation,
                    Type = RenewableAssetType.SolarPanel
                });
            }
            else
            {
                resultList.Add(new RenewableAssetDto
                {
                    Id = doc.Id,
                    Capacity = doc.Capacity,
                    Type = RenewableAssetType.RenewableAsset,
                    MeterPointId = doc.MeterPointId
                });
            }
        }

        return resultList;
    }

    public async Task<RenewableAsset?> GetByMeterPointIdAsync(long id)
    {
        var filter = Builders<RenewableAsset>.Filter.Eq(asset => asset.MeterPointId, id);
        return await context.RenewableAssets.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<string> CreateAsync(RenewableAsset newObj, CancellationToken cancellationToken = default)
    {
        try
        {
            await context.RenewableAssets.InsertOneAsync(newObj, cancellationToken: cancellationToken);
            return newObj.Id; // Returns the inserted object id
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Enhanced error handling
            var errorMessage = $"A duplicate key error occurred when inserting a new object. MeterPointId: {newObj.MeterPointId} is duplicated.";
            throw new DuplicateNameException(errorMessage, ex);
        }

    }

    public async Task RemoveAsync(string id)
    {
        var filter = Builders<RenewableAsset>.Filter.Eq(asset => asset.Id, id);
        await context.RenewableAssets.DeleteOneAsync(filter);
    }

    public async Task<RenewableAsset?> GetAsync(string id)
    {
        var filter = Builders<RenewableAsset>.Filter.Eq(asset => asset.Id, id);
        return await context.RenewableAssets.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<List<CurveDto>> SearchCurvesAsync(string? search, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RenewableAsset>.Filter.Empty;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmed = search.Trim();
            filter = Builders<RenewableAsset>.Filter.Regex(x => x.Name, new BsonRegularExpression(trimmed, "i"));
            if (long.TryParse(trimmed, out var meterPointId))
            {
                filter |= Builders<RenewableAsset>.Filter.Eq(x => x.MeterPointId, meterPointId);
            }
        }

        var documents = await context.RenewableAssets
            .Find(filter)
            .Limit(25)
            .ToListAsync(cancellationToken);

        return documents.Select(x => new CurveDto
        {
            Id = x.Id,
            Name = string.IsNullOrWhiteSpace(x.Name) ? x.MeterPointId.ToString() : x.Name,
            MeterPointId = x.MeterPointId,
            Capacity = x.Capacity,
            Type = x.Type
        }).ToList();
    }
}
