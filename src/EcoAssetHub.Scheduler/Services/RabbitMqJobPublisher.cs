using System.Text;
using System.Text.Json;
using EcoAssetHub.Domain.Models;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EcoAssetHub.Scheduler.Services;

public class RabbitMqJobPublisher(IOptions<RabbitMqOptions> options)
{
    private readonly RabbitMqOptions _options = options.Value;

    public void Publish(IngestionJobMessage message)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                PublishOnce(message);
                return;
            }
            catch when (attempt < 5)
            {
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }
    }

    private void PublishOnce(IngestionJobMessage message)
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.MessageId = message.JobId;
        properties.Type = nameof(IngestionJobMessage);

        channel.BasicPublish(exchange: string.Empty, routingKey: _options.QueueName, basicProperties: properties, body: body);
    }
}
