using EcoAssetHub.API.Application.RenewAbleCommands.GetQueries;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.UnitTest.Application.RenewAbleCommands.GetQueries;

public class GetAllQueryHandlerTests
{
    private readonly GetAllQueryHandler _handler;

    private readonly Mock<IRenewableAssetRepository> _mockRenewableAssetRepository;

    public GetAllQueryHandlerTests()
    {
        _mockRenewableAssetRepository = new Mock<IRenewableAssetRepository>();
        _handler = new GetAllQueryHandler(_mockRenewableAssetRepository.Object);
    }

    [Fact]
    public async Task Handle_ReturnsCombinedListOfAllRenewableAssets()
    {
        // Arrange
       
        var renewableAssets = new List<RenewableAssetDto>
        {
            new()
            {
                Id = Convert.ToString(100),
                MeterPointId = 100,
                Capacity = 100,
                Type = RenewableAssetType.RenewableAsset
            },
            new()
            {
                Id = Convert.ToString(200), MeterPointId = 200, Capacity = 100, Type = RenewableAssetType.WindTurbine,
                HubHeight = 200, RotorDiameter = 200
            },
            new()
            {
                Id = Convert.ToString(300),
                MeterPointId = 300,
                Capacity = 300,
                CompassOrientation = "North",
                Type = RenewableAssetType.SolarPanel
            }
        };


        _mockRenewableAssetRepository.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(renewableAssets);

        // Act
        var result = await _handler.Handle(new GetAllQuery(), new CancellationToken());

        // Assert
        Assert.NotNull(result);
        Assert.Equal( renewableAssets.Count, result.Count);
        _mockRenewableAssetRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
        // Additional assertions to check if the mapping to RenewAbleDto is correct
    }
}