using EcoAssetHub.Domain.Exceptions;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Infrastructure.Repositories;

public class SolarPanelRepository(EcoAssetHubContext context) : ISolarPanelRepository
{
    public async Task<List<SolarPanel>> GetAllAsync(RenewableFilter searchFilter)
    {
        var filter = Builders<SolarPanel>.Filter.Eq(asset => asset.Type, searchFilter.Type);

        return await context.RenewableAssets.OfType<SolarPanel>().Find(filter).ToListAsync();
    }

    public async Task<SolarPanel?> GetAsync(string id)
    {
        var filter = Builders<SolarPanel>.Filter.Eq(asset => asset.Id, id);
        return await context.RenewableAssets.OfType<SolarPanel>().Find(filter).FirstOrDefaultAsync();
    }

    public async Task<string> CreateAsync(SolarPanel newObj, CancellationToken cancellationToken = default)
    {
        try
        {
            await context.RenewableAssets.InsertOneAsync(newObj, cancellationToken: cancellationToken);
            return newObj.Id; // Returns the inserted object id
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Enhanced error handling
            var errorMessage =
                $"A duplicate key error occurred when inserting a new object. MeterPointId: {newObj.MeterPointId} is duplicated.";
            throw new DomainException(errorMessage, ex);
        }
    }

    public async Task UpdateAsync(SolarPanel updatedObject, CancellationToken cancellationToken = default)
    {
        var filter = Builders<RenewableAsset>.Filter.Eq(asset => asset.Id, updatedObject.Id);
        await context.RenewableAssets.ReplaceOneAsync(filter, updatedObject, cancellationToken: cancellationToken);
    }
}