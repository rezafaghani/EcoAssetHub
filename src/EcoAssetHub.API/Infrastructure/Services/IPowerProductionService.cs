namespace EcoAssetHub.API.Infrastructure.Services;

public interface IPowerProductionService
{
    public Task CreatePowerProduction(List<string> fileList, CancellationToken cancellationToken = default);
}