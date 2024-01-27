using EcoAssetHub.API.Infrastructure.Services;

namespace EcoAssetHub.API.Workers;

public class CreateProductionPriceWorker(ICacheService cacheService, ILogger<CreateProductionPriceWorker> logger)
    : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                
                var listOfDays = GetDates(2019, 9);
                listOfDays.AddRange(GetDates(2019, 10));
                listOfDays.AddRange(GetDates(2019, 11));
                foreach (var item in listOfDays)
                {
                    var existDate = cacheService.RetrieveByDateTime(item);
                    if (existDate == null || existDate.Price == 0)
                    {
                        Random rnd = new Random();
                        int decimalPlaces = 2; // Set the number of decimal places you need
                        var randomPrice = Math.Round((decimal)(10 + rnd.NextDouble()), decimalPlaces);
                        cacheService.Save(item, randomPrice);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception e)
        {
            logger.LogError("Fill price for each date is failed with error : {message}", e.Message);
        }
    }

    private static List<DateTime> GetDates(int year, int month)
    {
        return Enumerable.Range(1, DateTime.DaysInMonth(year, month))  // Days: 1, 2 ... 31 etc.
            .Select(day => new DateTime(year, month, day)) // Map each day to a date
            .ToList(); // Load dates into a list
    }
}