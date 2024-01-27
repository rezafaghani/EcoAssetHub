namespace EcoAssetHub.API.Infrastructure.Services.Dtos;

public class FileDataDto
{
    public required string FilePath { get; set; }
    public long MeterPointId { get; set; }
}