using TimeLens.Domain.Models;

namespace TimeLens.Domain.Interfaces;

public interface IExecutionRepository
{
    Task<List<ExecutionDefinitionDto>> GetDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<List<ExecutionDefinitionDto>> GetEnabledDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<ExecutionDefinitionDto?> GetDefinitionAsync(string id, CancellationToken cancellationToken = default);
    Task<ExecutionDefinitionDto> UpsertDefinitionAsync(UpsertExecutionDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<ExecutionDefinitionDto?> SetDefinitionEnabledAsync(string id, bool enabled, CancellationToken cancellationToken = default);
    Task MarkDefinitionQueuedAsync(string id, DateTimeOffset queuedAt, CancellationToken cancellationToken = default);
    Task<ExecutionRunDto> SaveRunAsync(string definitionId, string triggerType, DateTimeOffset start, DateTimeOffset end, List<ExecutionStepResultDto> results, CancellationToken cancellationToken = default);
    Task<List<ExecutionRunDto>> GetRunsAsync(string? definitionId, CancellationToken cancellationToken = default);
}
