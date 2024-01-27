namespace EcoAssetHub.Domain.Models;

public class PowerProductionFilter
{
    
    public long MeterPointId { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
}