using EcoAssetHub.API.Application.SolarPanelCommands.UpdateCommands;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Exceptions;
using EcoAssetHub.Domain.Interfaces;

namespace EcoAssetHub.UnitTest.Application.SolarPanelCommands.UpdateCommands
{
    public class UpdateSolarPanelCommandHandlerTests
    {
        private readonly Mock<ISolarPanelRepository> _mockRepository;
        private readonly UpdateSolarPanelCommandHandler _handler;
        private readonly SolarPanel _existingSolarPanel;
        private readonly string _id;
        public UpdateSolarPanelCommandHandlerTests()
        {
            _id = Guid.NewGuid().ToString();
            _mockRepository = new Mock<ISolarPanelRepository>();
            _handler = new UpdateSolarPanelCommandHandler(_mockRepository.Object);

            // Example data for an existing solar panel
            _existingSolarPanel = new SolarPanel(100, 100, "North")
            ;
        }

        [Fact]
        public async Task Handle_UpdatesSolarPanelCorrectly_WhenSolarPanelExists()
        {
            // Arrange
            var command = new UpdateSolarPanelCommand
            {
                Id = _id,
                Capacity = 150,
                CompassOrientation = "East"
                // Set other properties as needed
            };

            _mockRepository.Setup(repo => repo.GetAsync(_id))
                .ReturnsAsync(_existingSolarPanel);
             _mockRepository.Setup(repo => repo.UpdateAsync(It.IsAny<SolarPanel>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, new CancellationToken());

            // Assert
            _mockRepository.Verify(repo => repo.GetAsync(_id), Times.Once);
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<SolarPanel>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(command.Capacity, _existingSolarPanel.Capacity);
            Assert.Equal(command.CompassOrientation, _existingSolarPanel.CompassOrientation);
            // Assert other properties as needed
        }

        [Fact]
        public async Task Handle_ThrowsDomainException_WhenSolarPanelNotFound()
        {
            // Arrange
            var command = new UpdateSolarPanelCommand { Id = _id, CompassOrientation = "North" };
            _mockRepository.Setup(repo => repo.GetAsync(_id))
                .ReturnsAsync((SolarPanel)null);

            // Act & Assert
            await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(command, new CancellationToken()));
        }
    }

}