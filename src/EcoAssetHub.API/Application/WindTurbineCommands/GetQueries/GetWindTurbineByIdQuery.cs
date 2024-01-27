namespace EcoAssetHub.API.Application.WindTurbineCommands.GetQueries;

public class GetWindTurbineByIdQuery(string id) : IRequest<WindTurbineDto>
{
    public string Id { get; } = id;
}