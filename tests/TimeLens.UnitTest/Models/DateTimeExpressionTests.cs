using TimeLens.Domain.Models;

namespace TimeLens.UnitTest.Models;

public class DateTimeExpressionTests
{
    [Fact]
    public void Resolve_SupportsRelativeAndExactValues()
    {
        var now = new DateTimeOffset(2026, 7, 18, 14, 30, 0, TimeSpan.Zero);

        Assert.Equal(now, DateTimeExpression.Resolve("now", now));
        Assert.Equal(new DateTimeOffset(2026, 7, 17, 0, 0, 0, TimeSpan.Zero), DateTimeExpression.Resolve("today-1", now));
        Assert.Equal(new DateTimeOffset(2026, 7, 18, 12, 30, 0, TimeSpan.Zero), DateTimeExpression.Resolve("now-2h", now));
        Assert.Equal(new DateTimeOffset(2026, 7, 18, 10, 15, 0, TimeSpan.Zero), DateTimeExpression.Resolve("2026-07-18T10:15:00Z", now));
    }

    [Fact]
    public void Resolve_UsesTimezoneForTodayAndLocalExactTime()
    {
        var now = new DateTimeOffset(2026, 7, 18, 14, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateTimeOffset(2026, 7, 17, 22, 0, 0, TimeSpan.Zero), DateTimeExpression.Resolve("today", now, "Europe/Copenhagen"));
        Assert.Equal(new DateTimeOffset(2026, 7, 18, 8, 15, 0, TimeSpan.Zero), DateTimeExpression.Resolve("2026-07-18T10:15", now, "Europe/Copenhagen"));
    }
}
