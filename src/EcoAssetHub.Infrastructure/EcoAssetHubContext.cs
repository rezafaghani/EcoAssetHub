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

    public IMongoCollection<EnergyDataset> EnergyDatasets
        => _database.GetCollection<EnergyDataset>("EnergyDatasets");

    public IMongoCollection<EnergyTimeSeriesPoint> EnergyTimeSeriesPoints
        => _database.GetCollection<EnergyTimeSeriesPoint>("EnergyTimeSeriesPoints");

    public IMongoCollection<IngestionSchedule> IngestionSchedules
        => _database.GetCollection<IngestionSchedule>("IngestionSchedules");

    public IMongoCollection<IngestionJob> IngestionJobs
        => _database.GetCollection<IngestionJob>("IngestionJobs");

    public IMongoCollection<IngestionExecution> IngestionExecutions
        => _database.GetCollection<IngestionExecution>("IngestionExecutions");

    public async Task EnsureIndexesAsync(CancellationToken cancellationToken = default)
    {
        await RenewableAssets.Indexes.CreateOneAsync(
            new CreateIndexModel<RenewableAsset>(
                Builders<RenewableAsset>.IndexKeys.Ascending(asset => asset.MeterPointId),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        await RenewableAssets.Indexes.CreateOneAsync(
            new CreateIndexModel<RenewableAsset>(
                Builders<RenewableAsset>.IndexKeys.Ascending(asset => asset.Name)),
            cancellationToken: cancellationToken);

        await PowerProductions.Indexes.CreateOneAsync(
            new CreateIndexModel<PowerProduction>(
                Builders<PowerProduction>.IndexKeys
                    .Ascending(x => x.MeterPointId)
                    .Ascending(x => x.ProductionDateTime)
                    .Descending(x => x.AsOf)),
            cancellationToken: cancellationToken);

        await EnergyDatasets.Indexes.CreateOneAsync(
            new CreateIndexModel<EnergyDataset>(
                Builders<EnergyDataset>.IndexKeys
                    .Ascending(x => x.Source)
                    .Ascending(x => x.Endpoint)
                    .Ascending(x => x.Metric)
                    .Ascending(x => x.Country)
                    .Ascending(x => x.BiddingZone)
                    .Ascending(x => x.Region)),
            cancellationToken: cancellationToken);

        await EnergyTimeSeriesPoints.Indexes.CreateOneAsync(
            new CreateIndexModel<EnergyTimeSeriesPoint>(
                Builders<EnergyTimeSeriesPoint>.IndexKeys
                    .Ascending(x => x.DatasetId)
                    .Ascending(x => x.Timestamp)
                    .Descending(x => x.AsOf)),
            cancellationToken: cancellationToken);

        await IngestionSchedules.Indexes.CreateOneAsync(
            new CreateIndexModel<IngestionSchedule>(
                Builders<IngestionSchedule>.IndexKeys
                    .Ascending(x => x.Enabled)
                    .Ascending(x => x.CurveId)),
            cancellationToken: cancellationToken);

        await IngestionJobs.Indexes.CreateOneAsync(
            new CreateIndexModel<IngestionJob>(
                Builders<IngestionJob>.IndexKeys
                    .Ascending(x => x.ScheduleId)
                    .Descending(x => x.QueuedAt)),
            cancellationToken: cancellationToken);

        await IngestionExecutions.Indexes.CreateOneAsync(
            new CreateIndexModel<IngestionExecution>(
                Builders<IngestionExecution>.IndexKeys
                    .Ascending(x => x.JobId)
                    .Descending(x => x.CreatedAt)),
            cancellationToken: cancellationToken);
    }
}
