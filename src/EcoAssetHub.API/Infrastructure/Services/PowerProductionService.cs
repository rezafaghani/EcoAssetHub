using EcoAssetHub.API.Infrastructure.Services.Dtos;

namespace EcoAssetHub.API.Infrastructure.Services;

public class PowerProductionService(ILogger<PowerProductionService> logger, IProductionRepository productionRepository, ProductionFileReader fileReader, IRenewableAssetRepository renewableAssetRepository) : IPowerProductionService
{
    public async Task CreatePowerProduction(List<string> fileList, CancellationToken cancellationToken = default)
    {
        foreach (var fileItem in fileList)
        {
            if (IsCsvFile(fileItem))
            {
                await ProcessCsvFile(fileItem, cancellationToken);
            }
        }
    }

    private bool IsCsvFile(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private async Task ProcessCsvFile(string filePath, CancellationToken cancellationToken)
    {
        try
        {
            var meterPointId = ExtractMeterPointId(filePath);
            var productionList = await ReadProductionData(filePath, meterPointId);
            if (productionList.Any())
            {
                await EnsureRenewableAssetExists(meterPointId, cancellationToken);
                await SaveProductions(productionList, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    private long ExtractMeterPointId(string filePath)
    {
        long.TryParse(Path.GetFileNameWithoutExtension(filePath), out long meterPointId);
        return meterPointId;
    }

    private async Task<List<PowerProductionDto>> ReadProductionData(string filePath, long meterPointId)
    {
        return await fileReader.ReadData(new CsvFileDto
        {
            FilePath = filePath,
            MeterPointId = meterPointId
        });
    }

    private async Task EnsureRenewableAssetExists(long meterPointId, CancellationToken cancellationToken)
    {
        var existRenewable = await renewableAssetRepository.GetByMeterPointIdAsync(meterPointId);
        if (existRenewable == null)
        {
            await renewableAssetRepository.CreateAsync(new RenewableAsset(RenewableAssetType.RenewableAsset, 0, meterPointId), cancellationToken);
        }
    }

    private async Task SaveProductions(List<PowerProductionDto> productionList, CancellationToken cancellationToken)
    {
        var productions = productionList.Select(x => (PowerProduction)x).ToList();
        await productionRepository.CreateListAsync(productions, cancellationToken);
    }
}