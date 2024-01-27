using EcoAssetHub.API.Infrastructure.Services.Dtos;

namespace EcoAssetHub.API.Infrastructure.Services;

public abstract class ProductionFileReader
{
    public virtual Task<List<PowerProductionDto>> ReadData(CsvFileDto input)
    {
        throw new NotImplementedException();
    }
}