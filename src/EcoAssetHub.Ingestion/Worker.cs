using System.Text;
using System.Text.Json;
using EcoAssetHub.Domain.Models;
using EcoAssetHub.Ingestion.Grains;
using Microsoft.Extensions.Options;
using Orleans;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EcoAssetHub.Ingestion;

public class Worker(
    IGrainFactory grainFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RabbitMQ ingestion listener failed. Retrying in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        var rabbitOptions = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = rabbitOptions.HostName,
            Port = rabbitOptions.Port,
            UserName = rabbitOptions.UserName,
            Password = rabbitOptions.Password,
            DispatchConsumersAsync = true
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(rabbitOptions.QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var message = JsonSerializer.Deserialize<IngestionJobMessage>(json);
                if (message is null)
                {
                    channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
                    return;
                }

                var grain = grainFactory.GetGrain<IEnergyChartsDatasetGrain>(message.CurveId);
                await grain.IngestAsync(json, stoppingToken);
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process ingestion job message.");
                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
        };

        channel.BasicConsume(rabbitOptions.QueueName, autoAck: false, consumer);
        logger.LogInformation("Listening for ingestion jobs on RabbitMQ queue {QueueName}.", rabbitOptions.QueueName);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
