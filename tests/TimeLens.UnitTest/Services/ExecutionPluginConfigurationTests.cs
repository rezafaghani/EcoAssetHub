using TimeLens.Domain.Services;

namespace TimeLens.UnitTest.Services;

public class ExecutionPluginConfigurationTests
{
    [Theory]
    [InlineData("quarter-hour", 15)]
    [InlineData("hourly", 60)]
    [InlineData("daily", 1440)]
    [InlineData("PT15M", 15)]
    public void TryParseDuration_AcceptsDatasetGranularityLabels(string value, int minutes)
    {
        var parsed = ExecutionPluginConfiguration.TryParseDuration(value, out var duration);

        Assert.True(parsed);
        Assert.Equal(TimeSpan.FromMinutes(minutes), duration);
    }
}
