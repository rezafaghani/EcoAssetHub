using TimeLens.Domain.Models;

namespace TimeLens.Domain.Interfaces;

public interface IDatasetRepository
{
    Task<DatasetMetadataDto> UpsertAsync(DatasetMetadataDto metadata, CancellationToken cancellationToken = default);
    Task<DatasetMetadataDto?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<List<DatasetMetadataDto>> SearchAsync(DatasetSearchFilter filter, CancellationToken cancellationToken = default);
    Task<DatasetMetadataDto?> SetDeprecatedAsync(string id, bool deprecated, CancellationToken cancellationToken = default);
}
