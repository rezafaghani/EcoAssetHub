namespace TimeLens.Ingestion.Grains;

public interface IEnergyChartsDatasetGrain : IGrainWithStringKey
{
    Task IngestAsync(string messageJson, CancellationToken cancellationToken);
}
