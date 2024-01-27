using EcoAssetHub.Domain.Models;
using Microsoft.Extensions.Caching.Memory;

namespace EcoAssetHub.API.Infrastructure.Services;

public class CacheService(IMemoryCache cache) : ICacheService
{
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public void Save(DateTime dateTime, decimal value)
    {
        var key = dateTime.Date.ToString("yyyy-MM-dd");
        _cache.Set(key, value, TimeSpan.FromHours(1)); // Adjust expiration as needed
    }

    public ProductionPrice? RetrieveByDateTime(DateTime dateTime)
    {
        var key = dateTime.Date.ToString("yyyy-MM-dd");
        if (_cache.TryGetValue(key, out decimal price))
        {
            return new ProductionPrice
            {
                Timestamp = dateTime.Date,
                Price = price
            };
        }
        return null;
    }

    public Dictionary<int, decimal> RetrieveDateForMonth(DateTime startDate, DateTime endDateTime)
    {
        var monthlySums = new Dictionary<int, decimal>();
        for (DateTime date = startDate.Date; date <= endDateTime.Date; date = date.AddDays(1))
        {
            var key = date.ToString("yyyy-MM-dd");
            if (_cache.TryGetValue(key, out decimal price))
            {
                int monthKey = date.Month;
                if (!monthlySums.TryAdd(monthKey, price))
                {
                    monthlySums[monthKey] += price;
                }
            }
        }
        return monthlySums;
    }
}