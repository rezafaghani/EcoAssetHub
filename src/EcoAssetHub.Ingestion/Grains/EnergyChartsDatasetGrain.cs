using EcoAssetHub.Ingestion.Services;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Orleans;

namespace EcoAssetHub.Ingestion.Grains;

public class EnergyChartsDatasetGrain(
    EnergyChartsClient energyChartsClient,
    EnergyChartsNormalizer normalizer,
    IngestionWriteClient insertClient,
    IServiceScopeFactory scopeFactory,
    IOptions<IngestionOptions> options,
    ILogger<EnergyChartsDatasetGrain> logger) : Grain, IEnergyChartsDatasetGrain
{
    public async Task IngestAsync(string messageJson, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<IngestionJobMessage>(messageJson)
            ?? throw new InvalidOperationException("Ingestion job message payload is invalid.");

        using var runningScope = scopeFactory.CreateScope();
        var runningRepository = runningScope.ServiceProvider.GetRequiredService<IIngestionControlRepository>();
        await runningRepository.MarkJobRunningAsync(message.JobId, message.ExecutionId, cancellationToken);

        var inserted = 0;
        var skipped = 0;

        try
        {
            var definition = new EnergyChartsDatasetDefinition
            {
                Endpoint = message.Endpoint,
                Parameters = message.Parameters
            };

            var startExpression = string.IsNullOrWhiteSpace(message.WindowStartExpression)
                ? $"now-{Math.Max(message.LookbackHours > 0 ? message.LookbackHours : options.Value.LookbackHours, 1)}h"
                : message.WindowStartExpression;
            var endExpression = string.IsNullOrWhiteSpace(message.WindowEndExpression) ? "now" : message.WindowEndExpression;
            var effectiveDefinition = EnergyChartsDefaults.WithDateRange(definition, startExpression, endExpression);
            using var document = await energyChartsClient.GetAsync(effectiveDefinition, cancellationToken);
            var datasets = normalizer.Normalize(effectiveDefinition, document.RootElement);

            foreach (var dataset in datasets)
            {
                dataset.Metadata.CurveId = message.CurveId;
                using var metadataScope = scopeFactory.CreateScope();
                var metadataRepository = metadataScope.ServiceProvider.GetRequiredService<IDatasetRepository>();
                var saved = await metadataRepository.UpsertAsync(dataset.Metadata, cancellationToken);
                dataset.Batch.DatasetId = saved.Id;
                dataset.Batch.SourceMetadataVersion = saved.LastIngestedAt.ToUnixTimeSeconds().ToString();

                foreach (var chunk in dataset.Batch.Points.Chunk(Math.Max(message.BatchSize > 0 ? message.BatchSize : options.Value.BatchSize, 1)))
                {
                    var batch = new TimeSeriesBatchRequest
                    {
                        DatasetId = dataset.Batch.DatasetId,
                        SourceMetadataVersion = dataset.Batch.SourceMetadataVersion,
                        Points = chunk.ToList()
                    };
                    var result = await insertClient.InsertBatchAsync(batch, cancellationToken);
                    inserted += result.Inserted;
                    skipped += result.Skipped;
                    logger.LogInformation("Inserted {Inserted}, skipped {Skipped} for {DatasetId}.",
                        result.Inserted,
                        result.Skipped,
                        saved.Id);
                }
            }

            using var completedScope = scopeFactory.CreateScope();
            var completedRepository = completedScope.ServiceProvider.GetRequiredService<IIngestionControlRepository>();
            await completedRepository.MarkJobCompletedAsync(message.JobId, message.ExecutionId, inserted, skipped, cancellationToken);
        }
        catch (Exception ex)
        {
            using var failedScope = scopeFactory.CreateScope();
            var failedRepository = failedScope.ServiceProvider.GetRequiredService<IIngestionControlRepository>();
            await failedRepository.MarkJobFailedAsync(message.JobId, message.ExecutionId, ex.Message, CancellationToken.None);
            throw;
        }
    }
}
