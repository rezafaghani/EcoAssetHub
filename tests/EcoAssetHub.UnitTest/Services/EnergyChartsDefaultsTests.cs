using EcoAssetHub.Ingestion;
using EcoAssetHub.Ingestion.Services;

namespace EcoAssetHub.UnitTest.Services;

public class EnergyChartsDefaultsTests
{
    [Theory]
    [InlineData("/solar_share")]
    [InlineData("solar_share")]
    public void WithDateRange_DoesNotAddDatesForLatestOnlyEndpoint(string endpoint)
    {
        var definition = new EnergyChartsDatasetDefinition
        {
            Endpoint = endpoint,
            Parameters = new Dictionary<string, string> { ["country"] = "dk" }
        };

        var result = EnergyChartsDefaults.WithDateRange(definition, "today-10", "today");

        Assert.False(result.Parameters.ContainsKey("start"));
        Assert.False(result.Parameters.ContainsKey("end"));
    }

    [Theory]
    [InlineData("/public_power")]
    [InlineData("public_power")]
    public void WithDateRange_AddsDatesForRangeEndpoint(string endpoint)
    {
        var definition = new EnergyChartsDatasetDefinition
        {
            Endpoint = endpoint,
            Parameters = new Dictionary<string, string> { ["country"] = "dk" }
        };

        var result = EnergyChartsDefaults.WithDateRange(definition, "today-10", "today");

        Assert.True(result.Parameters.ContainsKey("start"));
        Assert.True(result.Parameters.ContainsKey("end"));
    }
}
