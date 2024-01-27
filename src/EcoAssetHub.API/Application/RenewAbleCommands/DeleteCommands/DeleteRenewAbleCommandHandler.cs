using EcoAssetHub.Domain.Exceptions;

namespace EcoAssetHub.API.Application.RenewAbleCommands.DeleteCommands;

public class DeleteRenewAbleCommandHandler(IRenewableAssetRepository repository)
    : IRequestHandler<DeleteRenewAbleCommand>
{
    public async Task Handle(DeleteRenewAbleCommand command, CancellationToken cancellationToken)
    {
        // Check if the solar panel exists
        var renewable = await repository.GetAsync(command.Id);
        if (renewable == null)
        {
            throw new DomainException($"Renewable Asset with ID {command.Id} not found.");
        }

        // Perform deletion
        await repository.RemoveAsync(command.Id);
    }
}