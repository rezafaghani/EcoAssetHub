using System.Text;
using System.Text.Json;
using EcoAssetHub.Domain.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EcoAssetHub.Scheduler.Services;

public class RabbitMqJobPublisher(IOptions<RabbitMqOptions> options)
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(IngestionJobMessage message, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                await PublishOnceAsync(message, cancellationToken);
                return;
            }
            catch when (attempt < 5)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private async Task PublishOnceAsync(IngestionJobMessage message, CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(_options.QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = message.JobId,
            Type = nameof(IngestionJobMessage)
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
