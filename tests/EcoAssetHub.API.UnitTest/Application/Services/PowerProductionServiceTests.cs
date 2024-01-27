using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.API.Infrastructure.Services.Dtos;
using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace EcoAssetHub.UnitTest.Application.Services
{
    public class PowerProductionServiceTests
    {
        private readonly Mock<IProductionRepository> _mockProductionRepository;
        private readonly Mock<CsvFileReader> _mockFileReader;
        private readonly Mock<IRenewableAssetRepository> _mockRenewableAssetRepository;
        private readonly PowerProductionService _service;

        public PowerProductionServiceTests()
        {
            _mockProductionRepository = new Mock<IProductionRepository>();
            _mockFileReader = new Mock<CsvFileReader>();
            _mockRenewableAssetRepository = new Mock<IRenewableAssetRepository>();
            Mock<ILogger<PowerProductionService>> mockLogger = new Mock<ILogger<PowerProductionService>>();
            _service = new PowerProductionService(mockLogger.Object, _mockProductionRepository.Object, _mockFileReader.Object, _mockRenewableAssetRepository.Object);
        }

        [Fact]
        public async Task CreatePowerProduction_WithValidCsvFile_CreatesProductionRecords()
        {
            // Arrange
            var fileList = new List<string> { "TestData/570715000000088747.csv" };
            var meterPointId = 570715000000088747; // Example MeterPointId
            var productionList = new List<PowerProductionDto>
            {
                new PowerProductionDto
                {
                    MeterPointId = meterPointId,
                    Production = 100,
                    ProductionDateTime = DateTime.Parse("06-10-2019 08:45")
                },
                new PowerProductionDto
                {
                    MeterPointId = meterPointId,
                    Production = 150,
                    ProductionDateTime = DateTime.Parse("06-10-2019 09:00")
                }
            };

            _mockFileReader.Setup(m => m.ReadData(It.IsAny<CsvFileDto>())).ReturnsAsync(productionList);
            _mockRenewableAssetRepository.Setup(x => x.GetByMeterPointIdAsync(meterPointId)).ReturnsAsync((RenewableAsset)null);

            // Act
            await _service.CreatePowerProduction(fileList);

            // Assert
            _mockFileReader.Verify(m => m.ReadData(It.IsAny<CsvFileDto>()), Times.Once);
            _mockRenewableAssetRepository.Verify(x => x.GetByMeterPointIdAsync(meterPointId), Times.Once);
            _mockProductionRepository.Verify(x => x.CreateListAsync(It.IsAny<List<PowerProduction>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}