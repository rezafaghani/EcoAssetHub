using System.ComponentModel.DataAnnotations;

namespace EcoAssetHub.API.Application.SolarPanelCommands.CreateCommands;

public class CreateSolarPanelCommand
{
    [Range(typeof(decimal), "0.0000000001", "79228162514264337593543950335")]
    public decimal Capacity { get; set; }

    [Range(1, long.MaxValue)]
    public long MeterPointId { get; set; }

    [Required]
    public required string CompassOrientation { get; set; }


    public static explicit operator SolarPanel(CreateSolarPanelCommand v)
    {
        return new SolarPanel(v.Capacity, v.MeterPointId, v.CompassOrientation);
    }
}
