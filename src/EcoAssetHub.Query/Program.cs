using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Infrastructure;
using EcoAssetHub.Infrastructure.Repositories;
using Scalar.AspNetCore;

var myAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:8080")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
builder.Services.AddScoped<EcoAssetHubContext>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration["DatabaseSettings:ConnectionString"] ?? throw new InvalidOperationException("Connection string is not configured.");
    var databaseName = configuration["DatabaseSettings:DatabaseName"] ?? throw new InvalidOperationException("Database name is not configured.");
    return new EcoAssetHubContext(connectionString, databaseName);
});
builder.Services.AddScoped<IProductionRepository, ProductionRepository>();
builder.Services.AddScoped<IRenewableAssetRepository, RenewableAssetRepository>();
builder.Services.AddScoped<IDatasetRepository, DatasetRepository>();
builder.Services.AddScoped<ITimeSeriesRepository, TimeSeriesRepository>();
builder.Services.AddScoped<IIngestionControlRepository, IngestionControlRepository>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<EcoAssetHubContext>().EnsureIndexesAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors(myAllowSpecificOrigins);
app.MapControllers();
app.Run();
