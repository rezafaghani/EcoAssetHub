using EcoAssetHub.API.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace EcoAssetHub.UnitTest.Infrastructure.Services;

public class CacheServiceTests
{
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly CacheService _cacheService;

    public CacheServiceTests()
    {
        _mockMemoryCache = new Mock<IMemoryCache>();
        _cacheService = new CacheService(_mockMemoryCache.Object);
       
    }

   
    [Fact]
    public void RetrieveByDateTime_ShouldReturnCorrectValue()
    {
        // Arrange
        var dateTime = DateTime.Now;
        var expectedPrice = 100.0m;
        var key = dateTime.Date.ToString("yyyy-MM-dd");
        object boxedPrice = expectedPrice;
        _mockMemoryCache.Setup(m => m.TryGetValue(key, out boxedPrice!)).Returns(true);

        // Act
        var result = _cacheService.RetrieveByDateTime(dateTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedPrice, result.Price);
    }

    [Fact]
    public void RetrieveByDateTime_ShouldReturnNullIfNotFound()
    {
        // Arrange
        var dateTime = DateTime.Now;
        var key = dateTime.Date.ToString("yyyy-MM-dd");
        object price;
        _mockMemoryCache.Setup(m => m.TryGetValue(key, out price!)).Returns(false);

        // Act
        var result = _cacheService.RetrieveByDateTime(dateTime);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void RetrieveDateForMonth_ShouldAggregateValues()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 31);
        var value = 10.0m;
        var expectedSum = 31 * value; // Assuming each day has the same price
        object boxedValue = value;
        _mockMemoryCache.Setup(m => m.TryGetValue(It.IsAny<string>(), out boxedValue!)).Returns(true);

        // Act
        var monthlySums = _cacheService.RetrieveDateForMonth(startDate, endDate);

        // Assert
        Assert.Single(monthlySums);
        Assert.Equal(expectedSum, monthlySums[1]);
    }



}