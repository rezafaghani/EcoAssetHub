namespace EcoAssetHub.API.Application.SolarPanelCommands.GetQueries
{
    public class GetSolarPanelByIdQuery : IRequest<SolarPanelDto>
    {
        public string Id { get; set; }

        public GetSolarPanelByIdQuery(string id)
        {
            Id = id;
        }
    }
}
