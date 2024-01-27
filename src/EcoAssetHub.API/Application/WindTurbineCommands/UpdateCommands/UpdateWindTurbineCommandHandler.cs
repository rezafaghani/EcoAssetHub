using EcoAssetHub.Domain.Exceptions;

namespace EcoAssetHub.API.Application.WindTurbineCommands.UpdateCommands;

public class UpdateWindTurbineCommandHandler(IWindTurbineRepository repository)
    : IRequestHandler<UpdateWindTurbineCommand>
{
    public async Task Handle(UpdateWindTurbineCommand command, CancellationToken cancellationToken)
    {
        var windTurbine = await repository.GetAsync(command.Id);
        if (windTurbine == null)
        {
            throw new DomainException($"Wind turbine with ID {command.Id} not found.");
        }

        // Update properties
        windTurbine.Capacity = command.Capacity;
        windTurbine.MeterPointId = command.MeterPointId;
        windTurbine.HubHeight = command.HubHeight;
        windTurbine.RotorDiameter = command.RotorDiameter;

        // Save changes
        await repository.UpdateAsync(windTurbine, cancellationToken);

    }
}