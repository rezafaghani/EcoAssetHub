using System.Text.Json;
using TimeLens.Ingestion;
using TimeLens.Ingestion.Services;

namespace TimeLens.UnitTest.Services;

public class EnergyChartsNormalizerTests
{
    private readonly EnergyChartsNormalizer _normalizer = new();

    [Theory]
    [InlineData("/public_power", "actual", "power")]
    [InlineData("/public_power_forecast", "forecast", "power")]
    [InlineData("/installed_power", "reference", "capacity")]
    [InlineData("/price", "actual", "price")]
    [InlineData("/cbet", "forecast", "exchange")]
    [InlineData("/ren_share_forecast", "forecast", "share")]
    [InlineData("/frequency", "actual", "frequency")]
    [InlineData("/signal", "forecast", "share")]
    public void Normalize_ClassifiesDataset(string endpoint, string dataKind, string category)
    {
        var datasets = Normalize(endpoint);

        Assert.NotEmpty(datasets);
        Assert.Equal(dataKind, datasets[0].Metadata.DataKind);
        Assert.Equal(category, datasets[0].Metadata.Category);
    }

    [Fact]
    public void Normalize_ShareEndpointSeparatesActualAndForecast()
    {
        var datasets = Normalize("/solar_share");

        Assert.Contains(datasets, x => x.Metadata.DataKind == "actual" && x.Metadata.Category == "share");
        Assert.Contains(datasets, x => x.Metadata.DataKind == "forecast" && x.Metadata.Category == "share");
    }

    [Fact]
    public void Normalize_SignalEndpointCategorizesTrafficSignal()
    {
        var datasets = Normalize("/signal");

        Assert.Contains(datasets, x => x.Metadata.Metric == "traffic_signal" && x.Metadata.DataKind == "forecast" && x.Metadata.Category == "signal");
    }

    private List<NormalizedDataset> Normalize(string endpoint)
    {
        using var document = JsonDocument.Parse(Json(endpoint));
        return _normalizer.Normalize(new EnergyChartsDatasetDefinition
        {
            Endpoint = endpoint,
            Parameters = new Dictionary<string, string>
            {
                ["country"] = "dk",
                ["production_type"] = "solar",
                ["forecast_type"] = "day-ahead"
            }
        }, document.RootElement);
    }

    private static string Json(string endpoint) => endpoint switch
    {
        "/public_power" => """
            {"unix_seconds":[1704067200,1704070800],"production_types":[{"name":"solar","data":[1,2]}]}
            """,
        "/public_power_forecast" => """
            {"unix_seconds":[1704067200,1704070800],"production_type":"solar","forecast_type":"day-ahead","forecast_values":[1,2]}
            """,
        "/installed_power" => """
            {"time":["2024","2025"],"production_types":[{"name":"solar","data":[1,2]}]}
            """,
        "/price" => """
            {"unix_seconds":[1704067200,1704070800],"unit":"EUR/MWh","price":[1,2]}
            """,
        "/cbet" => """
            {"unix_seconds":[1704067200,1704070800],"countries":[{"name":"DE","data":[1,2]}]}
            """,
        "/ren_share_forecast" => """
            {"unix_seconds":[1704067200,1704070800],"ren_share":[1,2],"solar_share":[1,2],"wind_onshore_share":[1,2],"wind_offshore_share":[1,2]}
            """,
        "/frequency" => """
            {"unix_seconds":[1704067200,1704070800],"data":[49.9,50.1]}
            """,
        "/signal" => """
            {"unix_seconds":[1704067200,1704070800],"share":[60,61],"signal":[1,2]}
            """,
        "/solar_share" => """
            {"unix_seconds":[1704067200,1704070800],"data":[20,21],"forecast":[22,23]}
            """,
        _ => "{}"
    };
}
