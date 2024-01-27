namespace EcoAssetHub.API.Application.WindTurbineCommands.CreateCommands;

public class CreateWindTurbineCommandHandler(IWindTurbineRepository repository)
    : IRequestHandler<CreateWindTurbineCommand, string>
{
    public async Task<string> Handle(CreateWindTurbineCommand request, CancellationToken cancellationToken)
    {
        var windTurbineId = await repository.CreateAsync((WindTurbine)request, cancellationToken);
        return windTurbineId;
    }
}