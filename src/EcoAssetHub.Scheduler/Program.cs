using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using EcoAssetHub.Infrastructure;
using EcoAssetHub.Infrastructure.Repositories;
using EcoAssetHub.Infrastructure.Services;
using EcoAssetHub.Scheduler;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection("Scheduler"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<EcoAssetHubContext>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var postgres = configuration.GetConnectionString("Postgres") ?? throw new InvalidOperationException("Postgres connection string is not configured.");
    return new EcoAssetHubContext(postgres, null);
});
builder.Services.AddScoped<IIngestionControlRepository, IngestionControlRepository>();
builder.Services.AddScoped<IDatasetRepository, DatasetRepository>();
builder.Services.AddSingleton<RabbitMqJobPublisher>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await InitializeAsync(host.Services);

host.Run();

static async Task InitializeAsync(IServiceProvider services)
{
    for (var attempt = 1; attempt <= 12; attempt++)
    {
        try
        {
            using var scope = services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<EcoAssetHubContext>().EnsurePostgresSchemaAsync();
            var schedules = DefaultSchedules.Create();
            var datasets = scope.ServiceProvider.GetRequiredService<IDatasetRepository>();
            foreach (var metadata in DefaultDatasetMetadata.Create(schedules))
            {
                await datasets.UpsertAsync(metadata, CancellationToken.None);
            }

            await scope.ServiceProvider
                .GetRequiredService<IIngestionControlRepository>()
                .EnsureDefaultSchedulesAsync(schedules, CancellationToken.None);
            return;
        }
        catch when (attempt < 12)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
