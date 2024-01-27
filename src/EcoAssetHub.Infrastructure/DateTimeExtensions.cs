namespace EcoAssetHub.Infrastructure;

public static class DateTimeExtensions
{
    public static DateTime ToDateTimeOffsetFromDateTime(this DateTime dateTime)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); // Replace "UTC" with "Central European Standard Time" if necessary
        var offset = timeZone.GetUtcOffset(dateTime);

        return (new DateTimeOffset(dateTime, offset)).DateTime;
    }
}