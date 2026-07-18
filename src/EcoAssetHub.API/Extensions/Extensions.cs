using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Infrastructure.Repositories;

namespace EcoAssetHub.API.Extensions;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.Services.AddScoped<EcoAssetHubContext>(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            if (configuration == null)
            {
                throw new InvalidOperationException("Configuration is not available.");
            }
            var connectionString = configuration["DatabaseSettings:ConnectionString"]??throw new InvalidOperationException("Connection string is not configured.");
            var databaseName = configuration["DatabaseSettings:DatabaseName"]??throw new InvalidOperationException("Database name is not configured.");
            return new EcoAssetHubContext(connectionString, databaseName);
        });
        services.AddScoped<IRenewableAssetRepository, RenewableAssetRepository>();
        services.AddScoped<IWindTurbineRepository, WindTurbineRepository>();
        services.AddScoped<ISolarPanelRepository, SolarPanelRepository>();
        services.AddScoped<IProductionRepository, ProductionRepository>();
        services.AddScoped<IDatasetRepository, DatasetRepository>();
        services.AddScoped<ITimeSeriesRepository, TimeSeriesRepository>();
        services.AddScoped<IIngestionControlRepository, IngestionControlRepository>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddMemoryCache();
    }
}
