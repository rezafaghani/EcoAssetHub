

namespace EcoAssetHub.API.Extensions;

// DatabaseInitializer.cs
// DatabaseInitializer.cs
public class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                // Retrieve the EcoAssetHubContext from the service provider
                var context = services.GetRequiredService<EcoAssetHubContext>();

                // Use the RenewableAssets collection from the context
                var collection = context.RenewableAssets;

                // Call the method to create a unique index
                await CreateUniqueIndexOnMeterPointIdAsync(collection);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<DatabaseInitializer>>();
                logger.LogError(ex, "An error occurred while creating the unique index on MeterPointId.");
            }
        }
    }

    private static async Task CreateUniqueIndexOnMeterPointIdAsync(IMongoCollection<RenewableAsset> collection)
    {
        var indexOptions = new CreateIndexOptions { Unique = true };
        var indexKeys = Builders<RenewableAsset>.IndexKeys.Ascending(asset => asset.MeterPointId);
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<RenewableAsset>(indexKeys, indexOptions));
    }
}

