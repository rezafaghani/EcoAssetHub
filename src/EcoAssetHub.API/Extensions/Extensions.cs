using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Infrastructure.Repositories;

namespace EcoAssetHub.API.Extensions;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;

        builder.Services.AddSingleton<EcoAssetHubContext>(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            if (configuration == null)
            {
                throw new InvalidOperationException("Configuration is not available.");
            }
            var postgres = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Postgres connection string is not configured.");
            var clickHouse = configuration.GetConnectionString("ClickHouse") ?? throw new InvalidOperationException("ClickHouse connection string is not configured.");
            return new EcoAssetHubContext(postgres, clickHouse);
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
