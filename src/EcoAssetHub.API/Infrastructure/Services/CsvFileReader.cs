using System.Globalization;
using System.Text;
using EcoAssetHub.API.Infrastructure.Services.Dtos;

namespace EcoAssetHub.API.Infrastructure.Services;

public class CsvFileReader : ProductionFileReader
{
    public override async Task<List<PowerProductionDto>> ReadData(CsvFileDto input)
    {

        try
        {
            var productionList = new List<PowerProductionDto>();

            using var reader = new StreamReader(input.FilePath);

            while (await reader.ReadLineAsync() is { } line)
            {
                var values = line.Split(',');

                // Check if the line has at least 2 elements
                if (values.Length < 2)
                {
                    // Handle the error, e.g., skip this line, log an error, etc.
                    continue; // This will skip to the next line
                }

                var result = ConvertDateTime(values[0], out var parDateTime);
                var productionValue = RemoveLeadingZeros(values[1]);
                if (result)
                {

                    var production = new PowerProductionDto
                    {
                        ProductionDateTime = parDateTime.ToDateTimeOffsetFromDateTime(),
                        Production = productionValue,
                        MeterPointId = input.MeterPointId
                    };

                    productionList.Add(production);
                }

            }

            return productionList;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private bool ConvertDateTime(string dateTimeString, out DateTime parsedDateTime)
    {


        // Split the string to extract the datetime part
        string[] parts = dateTimeString.Split(';');
        if (parts.Length > 0)
        {
            string cleanDateTimeString = parts[0];
            if (DateTime.TryParseExact(cleanDateTimeString, "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var convertDatetime))
            {
                parsedDateTime = convertDatetime;
                return true;
            }

            Console.WriteLine("Unable to parse the DateTime string.");
        }
        else
        {
            Console.WriteLine("Invalid input format.");
        }
        parsedDateTime = DateTime.MinValue;
        return false;

    }

    private static int RemoveLeadingZeros(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        StringBuilder result = new StringBuilder();
        bool leadingZeros = true;

        foreach (char c in input)
        {
            // Skip the leading zeros
            if (leadingZeros && c == '0')
                continue;

            // Once a non-zero character is encountered, set leadingZeros to false
            leadingZeros = false;
            result.Append(c);
        }

        return Convert.ToInt32(result.ToString());
    }
}