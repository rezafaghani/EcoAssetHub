using EcoAssetHub.Domain.Exceptions;
using SharpCompress.Archives;

namespace EcoAssetHub.API.Application.WindTurbineCommands.GetQueries;

public class GetWindTurbineByIdQueryHandler(IWindTurbineRepository repository)
    : IRequestHandler<GetWindTurbineByIdQuery, WindTurbineDto>
{
    public async Task<WindTurbineDto> Handle(GetWindTurbineByIdQuery query, CancellationToken cancellationToken)
    {
        var windTurbine = await repository.GetAsync(query.Id);
        if (windTurbine == null)
        {
            throw new DomainException($"Wind turbine with ID {query.Id} not found.");
        }

        // Map to WindTurbineDto (consider using AutoMapper)
        var windTurbineDto = new WindTurbineDto
        {
            Id = windTurbine.Id,
            Capacity = windTurbine.Capacity,
            MeterPointId = windTurbine.MeterPointId,
            HubHeight = windTurbine.HubHeight,
            RotorDiameter = windTurbine.RotorDiameter
            // Map other properties as needed
        };

        return windTurbineDto;
    }
}