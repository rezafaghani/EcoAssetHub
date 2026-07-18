using EcoAssetHub.Domain.Exceptions;
using EcoAssetHub.Domain.Models;
using Npgsql;

namespace EcoAssetHub.Infrastructure.Repositories;

public class WindTurbineRepository(EcoAssetHubContext context) : IWindTurbineRepository
{
    private readonly RenewableAssetRepository _assets = new(context);

    public Task<List<WindTurbine>> GetAllAsync(RenewableFilter searchFilter)
    {
        return _assets.GetByTypeAsync(RenewableAssetType.WindTurbine, reader =>
        {
            var turbine = new WindTurbine(reader.GetDecimal(3), reader.GetInt64(4), reader.GetDecimal(5), reader.GetDecimal(6))
            {
                Id = reader.GetString(0),
                Name = reader.GetString(2)
            };
            return turbine;
        });
    }

    public async Task<WindTurbine?> GetAsync(string id)
    {
        return await _assets.GetAsync(id) as WindTurbine;
    }

    public async Task<string> CreateAsync(WindTurbine newObj, CancellationToken cancellationToken = default)
    {
        try
        {
            await _assets.InsertAsync(newObj, cancellationToken);
            return newObj.Id;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new DomainException($"MeterPointId: {newObj.MeterPointId} is duplicated.", ex);
        }
    }

    public Task UpdateAsync(WindTurbine updatedObject, CancellationToken cancellationToken = default)
    {
        return _assets.UpdateAsync(updatedObject, cancellationToken);
    }

    public Task RemoveAsync(string id)
    {
        return _assets.RemoveAsync(id);
    }
}
