using EcoAssetHub.API.Infrastructure.Services;
using EcoAssetHub.API.Infrastructure.Services.Dtos;

namespace EcoAssetHub.UnitTest.Application.Services;

public class CsvFileReaderTests
{
    [Fact]
    public async Task ReadData_ShouldReturnCorrectData_WhenCsvIsValid()
    {
        // Arrange

        var mockFileReader = new Mock<CsvFileReader>();
        mockFileReader.Setup(m => m.ReadData(It.IsAny<CsvFileDto>())).ReturnsAsync([
            new PowerProductionDto
            {
                MeterPointId = 570715000000088747,
                Production = 100,
                ProductionDateTime = DateTime.Parse("06-10-2019 08:45")
            },

            new PowerProductionDto
            {
                MeterPointId = 570715000000088747,
                Production = 150,
                ProductionDateTime = DateTime.Parse("06-10-2019 09:00")
            }
        ]);

        // Act
        var result = await mockFileReader.Object.ReadData(new CsvFileDto { FilePath = "TestData/570715000000088747.csv" });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }
}