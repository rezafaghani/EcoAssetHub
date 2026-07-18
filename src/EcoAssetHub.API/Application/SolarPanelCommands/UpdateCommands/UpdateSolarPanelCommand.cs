using System.ComponentModel.DataAnnotations;

namespace EcoAssetHub.API.Application.SolarPanelCommands.UpdateCommands;

public class UpdateSolarPanelCommand
{
    [Required]
    public required string Id { get; set; } // ID of the solar panel to be updated

    [Range(typeof(decimal), "0.0000000001", "79228162514264337593543950335")]
    public decimal Capacity { get; set; }

    [Required]
    public required string CompassOrientation { get; set; }

}
