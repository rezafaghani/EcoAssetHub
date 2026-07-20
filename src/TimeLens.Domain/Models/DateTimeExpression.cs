using System.Text.RegularExpressions;

namespace TimeLens.Domain.Models;

public static class DateTimeExpression
{
    public static DateTimeOffset Resolve(string expression, DateTimeOffset? now = null, string? timeZone = null)
    {
        var zone = ResolveTimeZone(timeZone);
        if (HasOffset(expression) && DateTimeOffset.TryParse(expression, out var explicitTime))
        {
            return explicitTime.ToUniversalTime();
        }

        if (DateTime.TryParse(expression, out var localTime))
        {
            return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), zone));
        }

        var value = expression.Trim().ToLowerInvariant();
        var match = Regex.Match(value, "^(now|today)([+-]\\d+)?([hd])?$");
        if (!match.Success)
        {
            throw new FormatException($"Time expression '{expression}' is not supported.");
        }

        var reference = now ?? DateTimeOffset.UtcNow;
        var baseTime = match.Groups[1].Value == "today" ? StartOfDayUtc(reference, zone) : reference;
        if (!match.Groups[2].Success)
        {
            return baseTime;
        }

        var amount = int.Parse(match.Groups[2].Value);
        var unit = match.Groups[3].Success ? match.Groups[3].Value : "d";
        return unit == "h" ? baseTime.AddHours(amount) : AddLocalDays(baseTime, amount, zone);
    }

    public static bool TryResolve(string? expression, out DateTimeOffset value, string? timeZone = null)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        try
        {
            value = Resolve(expression, timeZone: timeZone);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException)
        {
            return false;
        }
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
        {
            return TimeZoneInfo.Utc;
        }

        var value = timeZone.Trim();
        return value.Equals("UTC", StringComparison.OrdinalIgnoreCase)
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.FindSystemTimeZoneById(value);
    }

    private static DateTimeOffset StartOfDayUtc(DateTimeOffset reference, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(reference, zone);
        var start = new DateTime(local.Year, local.Month, local.Day, 0, 0, 0, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(start, zone));
    }

    private static DateTimeOffset AddLocalDays(DateTimeOffset value, int days, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(value, zone).DateTime.AddDays(days);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), zone));
    }

    private static bool HasOffset(string expression)
    {
        return Regex.IsMatch(expression.Trim(), "(z|[+-]\\d{2}:?\\d{2})$", RegexOptions.IgnoreCase);
    }
}
