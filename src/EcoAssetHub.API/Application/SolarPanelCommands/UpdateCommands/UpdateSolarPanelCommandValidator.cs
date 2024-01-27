namespace EcoAssetHub.API.Application.SolarPanelCommands.UpdateCommands;

using FluentValidation;

public class UpdateSolarPanelCommandValidator : AbstractValidator<UpdateSolarPanelCommand>
{
    public UpdateSolarPanelCommandValidator()
    {
        RuleFor(cmd => cmd.Id).NotEmpty().WithMessage("ID is required.");
        RuleFor(cmd => cmd.Capacity).GreaterThan(0);
        RuleFor(cmd => cmd.CompassOrientation).NotEmpty(); 
    }
}
