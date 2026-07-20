using System.Text;
using System.Text.Json;
using TimeLens.Domain.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TimeLens.Infrastructure.Services;

public class RabbitMqJobPublisher(IOptions<RabbitMqOptions> options) : IValidationJobPublisher
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

    public async Task PublishValidationAsync(ValidationJobMessage message, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                await PublishOnceAsync(message, _options.ValidationQueueName, message.ExecutionId, nameof(ValidationJobMessage), cancellationToken);
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
        await PublishOnceAsync(message, _options.QueueName, message.JobId, nameof(IngestionJobMessage), cancellationToken);
    }

    private async Task PublishOnceAsync<T>(T message, string queueName, string messageId, string messageType, CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.UserName,
            Password = _options.Password
        };
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(_options.ExchangeName, ExchangeType.Direct, durable: true, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(queueName, _options.ExchangeName, queueName, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = new BasicProperties
        {
            Persistent = true,
            MessageId = messageId,
            Type = messageType
        };

        await channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
