using EcoAssetHub.API.Application.WindTurbineCommands.CreateCommands;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;

namespace EcoAssetHub.UnitTest.Application.WindTurbineCommands.CreateCommands
{
    public class CreateWindTurbineCommandHandlerTests
    { private readonly Mock<IWindTurbineRepository> _mockRepository;
        private readonly CreateWindTurbineCommandHandler _handler;
        private readonly string _expectedWindTurbineId;

        public CreateWindTurbineCommandHandlerTests()
        {
            _mockRepository = new Mock<IWindTurbineRepository>();
            _handler = new CreateWindTurbineCommandHandler(_mockRepository.Object);
            _expectedWindTurbineId = Guid.NewGuid().ToString();
        }

        [Fact]
        public async Task Handle_ReturnsCorrectWindTurbineId_WhenCalled()
        {
            // Arrange
            var command = new CreateWindTurbineCommand
            {
               Capacity = 100,
               HubHeight = 100,
               MeterPointId = 100,
               RotorDiameter = 100
            };

            _mockRepository.Setup(repo => repo.CreateAsync(It.IsAny<WindTurbine>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_expectedWindTurbineId);

            // Act
            var result = await _handler.Handle(command, new CancellationToken());

            // Assert
            Assert.Equal(_expectedWindTurbineId, result);
            _mockRepository.Verify(repo => repo.CreateAsync(It.IsAny<WindTurbine>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}