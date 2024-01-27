namespace EcoAssetHub.API.Application.SolarPanelCommands.CreateCommands;

using FluentValidation;

public class CreateSolarPanelCommandValidator : AbstractValidator<CreateSolarPanelCommand>
{
    public CreateSolarPanelCommandValidator()
    {
        RuleFor(cmd => cmd.Capacity).GreaterThan(0);
        RuleFor(cmd => cmd.MeterPointId).NotEmpty().GreaterThan(0);
        RuleFor(cmd => cmd.CompassOrientation).NotEmpty(); // Add more specific rules as needed
        // Add other validation rules here
    }
}
