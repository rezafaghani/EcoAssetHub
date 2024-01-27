using EcoAssetHub.API.Application.WindTurbineCommands.UpdateCommands;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Exceptions;
using EcoAssetHub.Domain.Interfaces;

namespace EcoAssetHub.UnitTest.Application.WindTurbineCommands.UpdateCommands
{
    public class UpdateWindTurbineCommandHandlerTests
    {
        private readonly Mock<IWindTurbineRepository> _mockRepository;
        private readonly UpdateWindTurbineCommandHandler _handler;
        private readonly WindTurbine _existingWindTurbine;
        private readonly string _id;

        public UpdateWindTurbineCommandHandlerTests()
        {
            _mockRepository = new Mock<IWindTurbineRepository>();
            _handler = new UpdateWindTurbineCommandHandler(_mockRepository.Object);
            _id = Guid.NewGuid().ToString();
            // Example data for an existing wind turbine
            _existingWindTurbine = new WindTurbine(200, 123, 80, 60);

        }

        [Fact]
        public async Task Handle_UpdatesWindTurbineCorrectly_WhenWindTurbineExists()
        {
            // Arrange
            var command = new UpdateWindTurbineCommand
            {
                Id = _id,
                Capacity = 250,
                MeterPointId = 456,
                HubHeight = 85,
                RotorDiameter = 65
            };

            _mockRepository.Setup(repo => repo.GetAsync(_id))
                .ReturnsAsync(_existingWindTurbine);
            _mockRepository.Setup(repo => repo.UpdateAsync(It.IsAny<WindTurbine>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _handler.Handle(command, new CancellationToken());

            // Assert
            _mockRepository.Verify(repo => repo.GetAsync(_id), Times.Once);
            _mockRepository.Verify(repo => repo.UpdateAsync(It.IsAny<WindTurbine>(), It.IsAny<CancellationToken>()), Times.Once);

            Assert.Equal(command.Capacity, _existingWindTurbine.Capacity);
            Assert.Equal(command.MeterPointId, _existingWindTurbine.MeterPointId);
            Assert.Equal(command.HubHeight, _existingWindTurbine.HubHeight);
            Assert.Equal(command.RotorDiameter, _existingWindTurbine.RotorDiameter);
        }

        [Fact]
        public async Task Handle_ThrowsDomainException_WhenWindTurbineNotFound()
        {
            // Arrange
            var command = new UpdateWindTurbineCommand { Id = _id };
            _mockRepository.Setup(repo => repo.GetAsync(_id))
                .ReturnsAsync((WindTurbine)null);

            // Act & Assert
            await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(command, new CancellationToken()));
        }
    }
}