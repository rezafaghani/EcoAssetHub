using EcoAssetHub.API.Application.Behaviors;
using EcoAssetHub.API.Application.SolarPanelCommands.CreateCommands;
using EcoAssetHub.API.Application.SolarPanelCommands.GetQueries;
using EcoAssetHub.API.Application.SolarPanelCommands.UpdateCommands;
using EcoAssetHub.API.Application.WindTurbineCommands.CreateCommands;
using EcoAssetHub.API.Application.WindTurbineCommands.GetQueries;
using EcoAssetHub.API.Application.WindTurbineCommands.UpdateCommands;
using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.Infrastructure.Repositories;

namespace EcoAssetHub.API.Extensions;

internal static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        var services = builder.Services;


        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining(typeof(Program));
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidatorBehavior<,>));
        });

        // Register the command validators for the validator behavior (validators based on FluentValidation library)
        //SolarPanelCommands Validator
        services.AddSingleton<IValidator<CreateSolarPanelCommand>, CreateSolarPanelCommandValidator>();
        services.AddSingleton<IValidator<GetSolarPanelByIdQuery>, GetSolarPanelByIdQueryValidator>();
        services.AddSingleton<IValidator<UpdateSolarPanelCommand>, UpdateSolarPanelCommandValidator>();

        //WindTurbineCommands Validator

        services.AddSingleton<IValidator<CreateWindTurbineCommand>, CreateWindTurbineCommandValidator>();
        services.AddSingleton<IValidator<UpdateWindTurbineCommand>, UpdateWindTurbineCommandValidator>();
        services.AddSingleton<IValidator<GetWindTurbineByIdQuery>, GetWindTurbineByIdQueryValidator>();


        builder.Services.AddScoped<EcoAssetHubContext>(sp =>
        {
            var configuration = sp.GetService<IConfiguration>();
            var connectionString = configuration["DatabaseSettings:ConnectionString"];
            var databaseName = configuration["DatabaseSettings:DatabaseName"];
            return new EcoAssetHubContext(connectionString, databaseName);
        });
        services.AddScoped<IRenewableAssetRepository, RenewableAssetRepository>();
        services.AddScoped<IWindTurbineRepository, WindTurbineRepository>();
        services.AddScoped<ISolarPanelRepository, SolarPanelRepository>();
        services.AddScoped<IProductionRepository, ProductionRepository>();
        services.AddScoped<ProductionFileReader, CsvFileReader>();
        services.AddScoped<IPowerProductionService, PowerProductionService>();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddMemoryCache();
    }
}