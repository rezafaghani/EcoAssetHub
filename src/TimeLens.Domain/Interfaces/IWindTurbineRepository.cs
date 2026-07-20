using TimeLens.Domain.Models;

namespace TimeLens.Domain.Interfaces;

public interface IWindTurbineRepository
{
    Task<List<WindTurbine>> GetAllAsync(RenewableFilter searchFilter);
    Task<WindTurbine?> GetAsync(string id);
    Task<string> CreateAsync(WindTurbine newObject, CancellationToken cancellationToken = default);
    Task UpdateAsync(WindTurbine updatedObject, CancellationToken cancellationToken = default);
    Task RemoveAsync(string id);
}