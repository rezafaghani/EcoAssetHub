namespace EcoAssetHub.Domain.Models;

public class PowerProductMonthlyDto
{
    public string MeterPointId { get; set; }
    public int Month { get; set; }
    public decimal Production { get; set; } // Assuming Value is a string like "2.38 DKK/kWh"

}