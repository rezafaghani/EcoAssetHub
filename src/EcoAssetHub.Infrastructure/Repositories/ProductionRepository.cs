using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Infrastructure.Repositories;

public class ProductionRepository(EcoAssetHubContext context) : IProductionRepository
{

    public async Task<List<PowerProductPerDayDto>> SpotPricesDaily(PowerProductionFilter searchFilter)
    {
        var filterBuilder = Builders<PowerProduction>.Filter;
        FilterDefinition<PowerProduction> filter;

        if (searchFilter.StartDateTime.Equals(searchFilter.EndDateTime))
        {
            // If the start and end dates are the same, use an equality filter
            filter = filterBuilder.Eq("ProductionDateTime.0", searchFilter.StartDateTime.Ticks) &
                     filterBuilder.Eq("MeterPointId", searchFilter.MeterPointId.ToString());
        }
        else
        {
            // If the start and end dates are different, use a range filter
            filter = filterBuilder.Gte("ProductionDateTime.0", searchFilter.StartDateTime.Ticks) &
                     filterBuilder.Lte("ProductionDateTime.0", searchFilter.EndDateTime.Ticks) &
                     filterBuilder.Eq("MeterPointId", searchFilter.MeterPointId);
        }



        var interVaList = await context.PowerProductions.Find(filter).ToListAsync();
        if (interVaList.Any())
        {


            var aggregatedData = interVaList
                .GroupBy(x => x.ProductionDateTime.Date)
                .Select(group => new PowerProductPerDayDto
                {
                    Start = group.Min(x => x.ProductionDateTime.DateTime),
                    End = group.Max(x => x.ProductionDateTime.DateTime),
                    Production = group.Sum(x => x.Production)
                })
                .ToList();
            return aggregatedData;
        }

        return new List<PowerProductPerDayDto>();
    }

    public async Task<List<PowerProductMonthlyDto>> SpotPriceMonthly(PowerProductionFilter searchFilter)
    {
        var filterBuilder = Builders<PowerProduction>.Filter;
        FilterDefinition<PowerProduction> filter;

        if (searchFilter.StartDateTime.Equals(searchFilter.EndDateTime))
        {
            // If the start and end dates are the same, use an equality filter
            filter = filterBuilder.Eq("ProductionDateTime.0", searchFilter.StartDateTime.Ticks);
        }
        else
        {
            // If the start and end dates are different, use a range filter
            filter = filterBuilder.Gte("ProductionDateTime.0", searchFilter.StartDateTime.Ticks) &
                     filterBuilder.Lte("ProductionDateTime.0", searchFilter.EndDateTime.Ticks);
        }
        var interVaList = await context.PowerProductions.Find(filter).ToListAsync();
        if (interVaList.Any())
        {
            var monthlyAggregatedData = interVaList
                .GroupBy(x => new { x.ProductionDateTime.Year, x.ProductionDateTime.Month, x.MeterPointId })
                .Select(group => new PowerProductMonthlyDto
                {
                    Month = group.Key.Month,
                    Production = group.Sum(x => x.Production),
                    MeterPointId = group.Key.MeterPointId
                })
                .ToList();
            return monthlyAggregatedData;
        }

        return new List<PowerProductMonthlyDto>();
    }

    public async Task<string> CreateAsync(PowerProduction input, CancellationToken cancellationToken = default)
    {
        var existData =
            await (await context.PowerProductions.FindAsync(
                x => x.MeterPointId == input.MeterPointId && x.ProductionDateTime == input.ProductionDateTime,
                cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
        if (existData != null)

        {
            await context.PowerProductions.InsertOneAsync(input, cancellationToken: cancellationToken);
            return input.Id; // Returns the inserted object id
        }

        return string.Empty;
    }

    public async Task CreateListAsync(List<PowerProduction> input, CancellationToken cancellationToken = default)
    {
        int successfulInserts = 0;
        int failedInserts = 0;
        int skippedInserts = 0;

        foreach (var data in input)
        {

            try
            {
                var filter = Builders<PowerProduction>.Filter.And(
                    Builders<PowerProduction>.Filter.Eq("MeterPointId", data.MeterPointId),
                    Builders<PowerProduction>.Filter.Eq("ProductionDateTime", data.ProductionDateTime));
                var existingRecord = await context.PowerProductions.Find(filter).FirstOrDefaultAsync(cancellationToken: cancellationToken);
                if (existingRecord == null)
                {
                    await context.PowerProductions.InsertOneAsync(data, cancellationToken: cancellationToken);
                    successfulInserts++;
                }
                else
                {
                    skippedInserts++;
                }

            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Log the duplicate key error and increment the failed inserts count
                Console.WriteLine($"Duplicate entry for record {data.MeterPointId} at {data.ProductionDateTime}: {ex.Message}");
                failedInserts++;
            }
            catch (Exception ex)
            {
                // Log other exceptions and increment the failed inserts count
                Console.WriteLine($"Error during insert for record {data.MeterPointId} at {data.ProductionDateTime}: {ex.Message}");
                failedInserts++;
            }
        }

        Console.WriteLine($"Total successful inserts: {successfulInserts}");
        Console.WriteLine($"Total failed inserts (due to duplicates or other errors): {failedInserts}");
        Console.WriteLine($"Total skipped inserts (existing records): {skippedInserts}");

    }

   
}