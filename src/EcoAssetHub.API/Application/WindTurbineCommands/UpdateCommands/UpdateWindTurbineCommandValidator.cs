namespace EcoAssetHub.API.Application.WindTurbineCommands.UpdateCommands;

public class UpdateWindTurbineCommandValidator : AbstractValidator<UpdateWindTurbineCommand>
{
    public UpdateWindTurbineCommandValidator()
    {
        RuleFor(cmd => cmd.Id).NotEmpty().WithMessage("ID is required.");
        RuleFor(cmd => cmd.Capacity).GreaterThan(0);
        RuleFor(cmd => cmd.MeterPointId).NotEmpty();
        RuleFor(cmd => cmd.HubHeight).GreaterThan(0);
        RuleFor(cmd => cmd.RotorDiameter).GreaterThan(0);
    }
}