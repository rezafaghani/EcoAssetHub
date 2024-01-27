using EcoAssetHub.API.Application.RenewAbleCommands.GetQueries;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.UnitTest.Application.RenewAbleCommands.GetQueries
{
    public class GetAllQueryHandlerTests
    {
        private readonly Mock<IWindTurbineRepository> _mockWindTurbineRepository;
        private readonly Mock<ISolarPanelRepository> _mockSolarPanelRepository;
        private readonly Mock<IRenewableAssetRepository> _mockRenewableAssetRepository;
        private readonly GetAllQueryHandler _handler;

        public GetAllQueryHandlerTests()
        {
            _mockWindTurbineRepository = new Mock<IWindTurbineRepository>();
            _mockSolarPanelRepository = new Mock<ISolarPanelRepository>();
            _mockRenewableAssetRepository = new Mock<IRenewableAssetRepository>();
            _handler = new GetAllQueryHandler(_mockRenewableAssetRepository.Object);
        }

        [Fact]
        public async Task Handle_ReturnsCombinedListOfAllRenewableAssets()
        {
            // Arrange
            var windTurbines = new List<WindTurbine>
        {
            new WindTurbine(100,100,100,100),
            new WindTurbine(200,200,200,200)
        };
            var solarPanels = new List<SolarPanel>
        {
            new SolarPanel(400,400,"North"),
            new SolarPanel(500,500,"North")
        };
            var renewableAssets = new List<RenewableAssetDto> { new RenewableAssetDto
            {
                Id = Convert.ToString(100),
                MeterPointId = 100,
                Capacity = 100
            }};

            _mockWindTurbineRepository.Setup(repo => repo.GetAllAsync(It.IsAny<RenewableFilter>()))
                .ReturnsAsync(windTurbines);
            _mockSolarPanelRepository.Setup(repo => repo.GetAllAsync(It.IsAny<RenewableFilter>()))
                .ReturnsAsync(solarPanels);
            _mockRenewableAssetRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(renewableAssets);

            // Act
            var result = await _handler.Handle(new GetAllQuery(), new CancellationToken());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(windTurbines.Count + solarPanels.Count + renewableAssets.Count, result.Count);
            _mockWindTurbineRepository.Verify(repo => repo.GetAllAsync(It.IsAny<RenewableFilter>()), Times.Once);
            _mockSolarPanelRepository.Verify(repo => repo.GetAllAsync(It.IsAny<RenewableFilter>()), Times.Once);
            _mockRenewableAssetRepository.Verify(repo => repo.GetAllAsync(), Times.Once);
            // Additional assertions to check if the mapping to RenewAbleDto is correct
        }
    }
}