using System.ComponentModel.DataAnnotations;

namespace EcoAssetHub.API.Application.WindTurbineCommands.CreateCommands;

public class CreateWindTurbineCommand
{
    [Range(typeof(decimal), "0.0000000001", "79228162514264337593543950335")]
    public decimal Capacity { get; set; }

    [Range(1, long.MaxValue)]
    public long MeterPointId { get; set; }

    [Range(typeof(decimal), "0.0000000001", "79228162514264337593543950335")]
    public decimal HubHeight { get; set; }

    [Range(typeof(decimal), "0.0000000001", "79228162514264337593543950335")]
    public decimal RotorDiameter { get; set; }

    public static explicit operator WindTurbine(CreateWindTurbineCommand v)
    {
        return new WindTurbine(v.Capacity, v.MeterPointId, v.HubHeight, v.RotorDiameter);
    }
}
