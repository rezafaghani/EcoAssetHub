using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.API.Infrastructure.Services;

public interface ICacheService
{
    void Save(DateTime dateTime, decimal value);
    ProductionPrice? RetrieveByDateTime(DateTime dateTime);
    Dictionary<int, decimal> RetrieveDateForMonth(DateTime startDate, DateTime endDateTime);
}