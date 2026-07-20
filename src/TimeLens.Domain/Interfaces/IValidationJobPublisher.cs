using TimeLens.Domain.Models;

namespace TimeLens.Domain.Interfaces;

public interface IValidationJobPublisher
{
    Task PublishValidationAsync(ValidationJobMessage message, CancellationToken cancellationToken = default);
}
