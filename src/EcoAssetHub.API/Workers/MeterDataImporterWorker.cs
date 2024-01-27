using EcoAssetHub.API.Infrastructure.Services;

namespace EcoAssetHub.API.Workers;

public class MeterDataImporterWorker(ILogger<MeterDataImporterWorker> logger, IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var myScopedService = scope.ServiceProvider.GetService<IPowerProductionService>();
        while (!stoppingToken.IsCancellationRequested)
        {
            //if (logger.IsEnabled(LogLevel.Information))
            //    logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            string[] fileEntries = Directory.GetFiles("FileData");
            await myScopedService?.CreatePowerProduction(fileEntries.ToList(), stoppingToken)!;
            await Task.Delay(1000, stoppingToken);
        }
    }
}