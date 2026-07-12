using EcoAssetHub.Ingestion;
using EcoAssetHub.Ingestion.Services;
using EcoAssetHub.Contracts;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Infrastructure;
using EcoAssetHub.Infrastructure.Repositories;
using Grpc.Net.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection("Ingestion"));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddScoped<EcoAssetHubContext>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration["DatabaseSettings:ConnectionString"] ?? throw new InvalidOperationException("Connection string is not configured.");
    var databaseName = configuration["DatabaseSettings:DatabaseName"] ?? throw new InvalidOperationException("Database name is not configured.");
    return new EcoAssetHubContext(connectionString, databaseName);
});
builder.Services.AddScoped<IIngestionControlRepository, IngestionControlRepository>();
builder.Services.AddHttpClient<EnergyChartsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["EnergyCharts:BaseUrl"] ?? "https://api.energy-charts.info/");
});
builder.Services.AddSingleton<EnergyChartsRateLimiter>();
builder.Services.AddHttpClient<OAuthTokenProvider>();
builder.Services.AddSingleton<EnergyChartsNormalizer>();
builder.Services.AddScoped<IngestionWriteClient>();
var grpcClientBuilder = builder.Services.AddGrpcClient<IngestionWrite.IngestionWriteClient>(options =>
{
    options.Address = new Uri(builder.Configuration["InsertApi:GrpcUrl"] ?? "http://localhost:5103");
})
.ConfigureChannel(options =>
{
    var grpcUrl = builder.Configuration["InsertApi:GrpcUrl"] ?? "http://localhost:5103";
    options.UnsafeUseInsecureChannelCallCredentials = grpcUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
});

if (!string.IsNullOrWhiteSpace(builder.Configuration["InsertApi:TokenEndpoint"]))
{
    grpcClientBuilder.AddCallCredentials(async (context, metadata, serviceProvider) =>
    {
        var tokenProvider = serviceProvider.GetRequiredService<OAuthTokenProvider>();
        var token = await tokenProvider.GetTokenAsync(context.CancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            metadata.Add("Authorization", $"Bearer {token}");
        }
    });
}
builder.UseOrleans(siloBuilder =>
{
    siloBuilder.UseLocalhostClustering();
});
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
            await scope.ServiceProvider.GetRequiredService<EcoAssetHubContext>().EnsureIndexesAsync();
            return;
        }
        catch when (attempt < 12)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}
