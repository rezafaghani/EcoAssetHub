namespace EcoAssetHub.Infrastructure;

using MongoDB.Driver;
using Domain.Entities;

public class EcoAssetHubContext
{
    private readonly IMongoDatabase _database;

    public EcoAssetHubContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<PowerProduction> PowerProductions 
        => _database.GetCollection<PowerProduction>("PowerProductions");

    public IMongoCollection<RenewableAsset> RenewableAssets 
        => _database.GetCollection<RenewableAsset>("RenewableAssets");

    // Other collections can be added here as needed
}