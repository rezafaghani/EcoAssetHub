

namespace EcoAssetHub.API.Application.WindTurbineCommands.CreateCommands;

public class CreateWindTurbineCommandValidator :AbstractValidator<CreateWindTurbineCommand>
{
    public CreateWindTurbineCommandValidator()
    {
        RuleFor(cmd => cmd.Capacity).GreaterThan(0);
        RuleFor(cmd => cmd.MeterPointId).NotEmpty();
        RuleFor(cmd => cmd.HubHeight).NotEmpty(); 
        RuleFor(cmd => cmd.RotorDiameter).NotEmpty(); 
    }
}