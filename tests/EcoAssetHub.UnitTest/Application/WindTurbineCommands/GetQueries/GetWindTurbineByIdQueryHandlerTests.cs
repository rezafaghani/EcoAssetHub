using EcoAssetHub.API.Application.WindTurbineCommands.GetQueries;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Exceptions;
using EcoAssetHub.Domain.Interfaces;

namespace EcoAssetHub.UnitTest.Application.WindTurbineCommands.GetQueries
{
    public class GetWindTurbineByIdQueryHandlerTests
    {
        private readonly Mock<IWindTurbineRepository> _mockRepository;
        private readonly GetWindTurbineByIdQueryHandler _handler;
        private readonly WindTurbine _expectedWindTurbine;
        private readonly string _id;
        public GetWindTurbineByIdQueryHandlerTests()
        {
            _mockRepository = new Mock<IWindTurbineRepository>();
            _handler = new GetWindTurbineByIdQueryHandler(_mockRepository.Object);
            _id = Guid.NewGuid().ToString();
            // Example data for a wind turbine
            _expectedWindTurbine = new WindTurbine(200, 123, 80, 60);
        }

        [Fact]
        public async Task Handle_ReturnsCorrectWindTurbineDto_WhenWindTurbineExists()
        {
            // Arrange
            var query = new GetWindTurbineByIdQuery(_id);
            _mockRepository.Setup(repo => repo.GetAsync(_id))
                .ReturnsAsync(_expectedWindTurbine);

            // Act
            var result = await _handler.Handle(query, new CancellationToken());

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_expectedWindTurbine.Id, result.Id);
            Assert.Equal(_expectedWindTurbine.Capacity, result.Capacity);
            Assert.Equal(_expectedWindTurbine.MeterPointId, result.MeterPointId);
            Assert.Equal(_expectedWindTurbine.HubHeight, result.HubHeight);
            Assert.Equal(_expectedWindTurbine.RotorDiameter, result.RotorDiameter);
            // Assert other properties as needed
        }

        [Fact]
        public async Task Handle_ThrowsDomainException_WhenWindTurbineNotFound()
        {
            // Arrange
            var query = new GetWindTurbineByIdQuery (_id);
            _mockRepository.Setup(repo => repo.GetAsync(_id))
                .ReturnsAsync((WindTurbine)null);

            // Act & Assert
            await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(query, new CancellationToken()));
        }
    }
}