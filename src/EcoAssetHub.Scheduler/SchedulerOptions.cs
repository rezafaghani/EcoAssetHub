namespace EcoAssetHub.Scheduler;

public class SchedulerOptions
{
    public int PollingSeconds { get; set; } = 30;
    public string QualityApiBaseUrl { get; set; } = string.Empty;
}
