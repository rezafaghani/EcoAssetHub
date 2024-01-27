using EcoAssetHub.API.Application.SolarPanelCommands.CreateCommands;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;

namespace EcoAssetHub.UnitTest.Application.SolarPanelCommands.CreateCommands
{
    public class CreateSolarPanelCommandHandlerTests
    {
        private readonly Mock<ISolarPanelRepository> _mockSolarPanelRepository;
        private readonly CreateSolarPanelCommandHandler _handler;

        public CreateSolarPanelCommandHandlerTests()
        {
            _mockSolarPanelRepository = new Mock<ISolarPanelRepository>();
            _handler = new CreateSolarPanelCommandHandler(_mockSolarPanelRepository.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ReturnsSolarPanelId()
        {
            // Arrange
            var command = new CreateSolarPanelCommand
            {
                // Initialize the command properties
                Capacity = 1,
                CompassOrientation = "Left",
                MeterPointId = 570715000000088747
            };
            var expectedSolarPanelId =Guid.NewGuid().ToString(); // Example ID to be returned by the repository

            _mockSolarPanelRepository.Setup(repo => repo.CreateAsync(It.IsAny<SolarPanel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedSolarPanelId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(expectedSolarPanelId, result);
            _mockSolarPanelRepository.Verify(repo => repo.CreateAsync(It.IsAny<SolarPanel>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}