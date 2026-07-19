namespace EcoAssetHub.Scheduler;

public class SchedulerOptions
{
    public int PollingSeconds { get; set; } = 30;
    public string ExecutionApiBaseUrl { get; set; } = string.Empty;
    public string QualityApiBaseUrl { get; set; } = string.Empty;
}
