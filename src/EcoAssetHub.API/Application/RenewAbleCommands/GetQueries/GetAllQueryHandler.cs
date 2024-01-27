namespace EcoAssetHub.API.Application.RenewAbleCommands.GetQueries;

public class GetAllQueryHandler(IRenewableAssetRepository renewableRepository)
    : IRequestHandler<GetAllQuery, List<RenewAbleDto>>
{
    public async Task<List<RenewAbleDto>> Handle(GetAllQuery request, CancellationToken cancellationToken)
    {
       
        var renewableAssets = await renewableRepository.GetAllAsync();

        
        
        var renewAbleList = renewableAssets.Select(x => new RenewAbleDto
        {
            Id = x.Id,
            Capacity = x.Capacity,
            MeterPointId = x.MeterPointId,
            Type = x.Type,
            CompassOrientation = x.CompassOrientation,
            RotorDiameter = x.RotorDiameter,
            HubHeight = x.HubHeight
        }).ToList();
        return renewAbleList;
       
    }
}