namespace EcoAssetHub.API.Extensions;

public class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<EcoAssetHubContext>();
            await context.EnsureIndexesAsync();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<DatabaseInitializer>>();
            logger.LogError(ex, "An error occurred while creating database indexes.");
        }
    }
}
