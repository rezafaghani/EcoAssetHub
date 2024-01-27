namespace EcoAssetHub.API.Application.SolarPanelCommands.CreateCommands;

using MediatR;

public class CreateSolarPanelCommandHandler(ISolarPanelRepository repository)
    : IRequestHandler<CreateSolarPanelCommand, string> // Returns only the ID
{
    // Replace with your actual repository interface

    public async Task<string> Handle(CreateSolarPanelCommand command, CancellationToken cancellationToken)
    {
        var solarPanelId = await repository.CreateAsync((SolarPanel)command, cancellationToken);

        return solarPanelId; // Assuming Id is set by the repository upon creation
    }
}