using EcoAssetHub.API.Application.SolarPanelCommands.GetQueries;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Exceptions;
using EcoAssetHub.Domain.Interfaces;

namespace EcoAssetHub.UnitTest.Application.SolarPanelCommands.GetQueries
{
    public class GetSolarPanelByIdQueryHandlerTests
    {
        private readonly Mock<ISolarPanelRepository> _mockRepository;
        private readonly GetSolarPanelByIdQueryHandler _handler;
        private readonly SolarPanel _expectedSolarPanel;
        private readonly string _id;

        public GetSolarPanelByIdQueryHandlerTests()
        {
            _mockRepository = new Mock<ISolarPanelRepository>();
            _handler = new GetSolarPanelByIdQueryHandler(_mockRepository.Object);
            _id = Guid.NewGuid().ToString();
            // Example data for a solar panel
            _expectedSolarPanel = new SolarPanel(100, 100, "North");
        }

        [Fact]
        public async Task Handle_ReturnsCorrectSolarPanelDto_WhenSolarPanelExists()
        {
            // Arrange
            var query = new GetSolarPanelByIdQuery(_id);
            _mockRepository.Setup(repo => repo.GetAsync(_id))
                .ReturnsAsync(_expectedSolarPanel);

            // Act
            var result = await _handler.Handle(query, new CancellationToken());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_expectedSolarPanel.Id, result.Id);
            Assert.Equal(_expectedSolarPanel.Capacity, result.Capacity);
            Assert.Equal(_expectedSolarPanel.CompassOrientation, result.CompassOrientation);
            Assert.Equal(_expectedSolarPanel.MeterPointId, result.MeterPointId);
        }

        [Fact]
        public async Task Handle_ThrowsDomainException_WhenSolarPanelNotFound()
        {
            // Arrange
            var query = new GetSolarPanelByIdQuery(_id);
            _ = _mockRepository.Setup(repo => repo.GetAsync(_id))
                  .ReturnsAsync((SolarPanel)null);

            // Act & Assert
            await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(query, new CancellationToken()));
        }
    }
}