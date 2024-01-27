using EcoAssetHub.Domain.Exceptions;

namespace EcoAssetHub.API.Application.SolarPanelCommands.GetQueries;

public class GetSolarPanelByIdQueryHandler(ISolarPanelRepository repository)
    : IRequestHandler<GetSolarPanelByIdQuery, SolarPanelDto>
{
    public async Task<SolarPanelDto> Handle(GetSolarPanelByIdQuery query, CancellationToken cancellationToken)
    {
        var solarPanel = await repository.GetAsync(query.Id);
        if (solarPanel == null)
        {
            throw new DomainException($"Solar panel with ID {query.Id} not found.");
        }

        // Map to SolarPanelDto (consider using AutoMapper)
        var solarPanelDto = new SolarPanelDto
        {
           Capacity = solarPanel.Capacity,
           CompassOrientation = solarPanel.CompassOrientation,
           Id = solarPanel.Id,
           MeterPointId = solarPanel.MeterPointId

        };

        return solarPanelDto;
    }
}