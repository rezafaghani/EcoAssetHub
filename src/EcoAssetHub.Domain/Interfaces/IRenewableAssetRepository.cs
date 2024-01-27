using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Domain.Interfaces;

public interface IRenewableAssetRepository
{
    Task<List<RenewableAssetDto>> GetAllAsync();
    Task<RenewableAsset?> GetByMeterPointIdAsync(long id);
    Task<string> CreateAsync(RenewableAsset newObject, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id);
    Task<RenewableAsset?> GetAsync(string id);
}