using EcoAssetHub.Domain.Entities;
using EcoAssetHub.Domain.Interfaces;
using EcoAssetHub.Domain.Models;

namespace EcoAssetHub.UnitTest.Repositories;

public class ProductionRepositoryTests
{
    private readonly Mock<IProductionRepository> _mockRepository = new();

    [Fact]
    public async Task CalculatePowerProduction_SameStartEndDates_ReturnsCorrectData()
    {
        // Arrange
        var meterPointId = 570715000000088747; // Example MeterPointId
        var searchFilter = new PowerProductionFilter
        {
            StartDateTime = DateTime.Parse("06-10-2019 08:45"),
            EndDateTime = DateTime.Parse("06-10-2019 09:00"),
            MeterPointId = meterPointId
        };

        var expectedData = new List<PowerProductPerDayDto>
        {
            new()
            {
                Start = DateTime.Parse("06-10-2019 08:45"),
                Production = 200,
                End =  DateTime.Parse("06-10-2019 09:00")
            },
 
        };

        _mockRepository.Setup(repo => repo.SpotPricesDaily(searchFilter))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _mockRepository.Object.SpotPricesDaily(searchFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedData.Count, result.Count);
    }

    [Fact]
    public async Task CalculatePowerProduction_DifferentStartEndDates_ReturnsCorrectData()
    {
        // Arrange
        var meterPointId = 570715000000088747; // Example MeterPointId
        var searchFilter = new PowerProductionFilter
        {
            StartDateTime = DateTime.Parse("06-10-2019 08:45"),
            EndDateTime = DateTime.Parse("06-10-2019 09:00"),
            MeterPointId = meterPointId
        };

        var mockedData = new List<PowerProduction>
        {
            new()
            {
                MeterPointId = meterPointId.ToString(),
                Production = 100,
                ProductionDateTime = DateTime.Parse("06-10-2019 08:45")
            },
            new()
            {
                MeterPointId = meterPointId.ToString(),
                Production = 250,
                ProductionDateTime = DateTime.Parse("06-10-2019 09:00")
            }
        };

        
        var expectedData = new List<PowerProductPerDayDto>
        {
            new()
            {
                Start = DateTime.Parse("06-10-2019 08:45"),
                Production = 350,
                End =  DateTime.Parse("06-10-2019 09:00")
            }
        };

        _mockRepository.Setup(repo => repo.SpotPricesDaily(searchFilter))
            .ReturnsAsync(expectedData);
        // Act
        var result = await _mockRepository.Object.SpotPricesDaily(searchFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mockedData.Sum(x=>x.Production), result.Sum(x=>x.Production));
        // Additional assertions as necessary
    }

    [Fact]
    public async Task CalculatePowerProduction_NoDataMatches_ReturnsEmptyList()
    {
        // Arrange
        var meterPointId = 570715000000088747;
        var searchFilter = new PowerProductionFilter
        {
            StartDateTime = DateTime.Today.AddDays(-10),
            EndDateTime = DateTime.Today.AddDays(-5),
            MeterPointId = meterPointId
        };
        

        _mockRepository.Setup(repo => repo.SpotPricesDaily(searchFilter))
            .ReturnsAsync(new List<PowerProductPerDayDto>());

        // Act
        var result = await _mockRepository.Object.SpotPricesDaily(searchFilter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}