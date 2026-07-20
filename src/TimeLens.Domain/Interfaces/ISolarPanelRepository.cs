using TimeLens.Domain.Models;

namespace TimeLens.Domain.Interfaces;

public interface ISolarPanelRepository
{
    Task<List<SolarPanel>> GetAllAsync(RenewableFilter searchFilter);
    Task<SolarPanel?> GetAsync(string id);
    Task<string> CreateAsync(SolarPanel newObject, CancellationToken cancellationToken = default);
    Task UpdateAsync(SolarPanel updatedObject, CancellationToken cancellationToken = default);
    
}