namespace EcoAssetHub.API.Application.RenewAbleCommands.DeleteCommands
{
    using MediatR;

    public class DeleteRenewAbleCommand : IRequest
    {
        public string Id { get; }

        public DeleteRenewAbleCommand(string id)
        {
            Id = id;
        }
    }

}
