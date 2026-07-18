using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Infrastructure;
using EcoAssetHub.Infrastructure.Repositories;
using EcoAssetHub.Scheduler;
using EcoAssetHub.Scheduler.Services;

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
            await scope.ServiceProvider
                .GetRequiredService<IIngestionControlRepository>()
                .EnsureDefaultSchedulesAsync(DefaultSchedules.Create(), CancellationToken.None);
            return;
        }
        catch when (attempt < 12)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
