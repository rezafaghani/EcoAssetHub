using EcoAssetHub.Domain.Exceptions;

namespace EcoAssetHub.API.Application.SolarPanelCommands.UpdateCommands;


public class UpdateSolarPanelCommandHandler(ISolarPanelRepository repository) : IRequestHandler<UpdateSolarPanelCommand>
{
    public async Task Handle(UpdateSolarPanelCommand command, CancellationToken cancellationToken)
    {
        // Retrieve existing solar panel
        var solarPanel = await repository.GetAsync(command.Id);
        if (solarPanel == null)
        {
            throw new DomainException($"Solar panel with ID {command.Id} not found.");
        }

        // Update properties
        solarPanel.Capacity = command.Capacity;
        solarPanel.CompassOrientation = command.CompassOrientation;
        // Update other properties as needed

        // Save changes
        await repository.UpdateAsync(solarPanel, cancellationToken: cancellationToken);

    }
}
