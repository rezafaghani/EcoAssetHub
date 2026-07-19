namespace EcoAssetHub.Domain.Models;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "ecoassethub";
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = string.Empty;
    public string ExchangeName { get; set; } = "ecoassethub.jobs";
    public string QueueName { get; set; } = "ecoassethub.ingestion.jobs";
    public string ValidationQueueName { get; set; } = "ecoassethub.validation.jobs";
}
