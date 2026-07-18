using EcoAssetHub.Domain.Exceptions;
using EcoAssetHub.Domain.Models;
using Npgsql;

namespace EcoAssetHub.Infrastructure.Repositories;

public class SolarPanelRepository(EcoAssetHubContext context) : ISolarPanelRepository
{
    private readonly RenewableAssetRepository _assets = new(context);

    public Task<List<SolarPanel>> GetAllAsync(RenewableFilter searchFilter)
    {
        return _assets.GetByTypeAsync(RenewableAssetType.SolarPanel, reader =>
        {
            var panel = new SolarPanel(reader.GetDecimal(3), reader.GetInt64(4), reader.GetString(7))
            {
                Id = reader.GetString(0),
                Name = reader.GetString(2)
            };
            return panel;
        });
    }

    public async Task<SolarPanel?> GetAsync(string id)
    {
        return await _assets.GetAsync(id) as SolarPanel;
    }

    public async Task<string> CreateAsync(SolarPanel newObj, CancellationToken cancellationToken = default)
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

    public Task UpdateAsync(SolarPanel updatedObject, CancellationToken cancellationToken = default)
    {
        return _assets.UpdateAsync(updatedObject, cancellationToken);
    }
}
