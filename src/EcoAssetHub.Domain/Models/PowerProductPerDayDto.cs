namespace EcoAssetHub.Domain.Models
{
    public class PowerProductPerDayDto
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public decimal Production { get; set; } // Assuming Value is a string like "2.38 DKK/kWh"
    }
}
