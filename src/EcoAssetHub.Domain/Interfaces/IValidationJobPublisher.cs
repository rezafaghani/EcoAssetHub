using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.Domain.Interfaces;

public interface IValidationJobPublisher
{
    Task PublishValidationAsync(ValidationJobMessage message, CancellationToken cancellationToken = default);
}
