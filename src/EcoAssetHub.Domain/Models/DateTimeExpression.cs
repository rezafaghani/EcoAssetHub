using System.Text.RegularExpressions;

namespace EcoAssetHub.Domain.Models;

public static class DateTimeExpression
{
    public static DateTimeOffset Resolve(string expression, DateTimeOffset? now = null)
    {
        if (DateTimeOffset.TryParse(expression, out var explicitTime))
        {
            return explicitTime.ToUniversalTime();
        }

        var value = expression.Trim().ToLowerInvariant();
        var match = Regex.Match(value, "^(now|today)([+-]\\d+)?([hd])?$");
        if (!match.Success)
        {
            throw new FormatException($"Time expression '{expression}' is not supported.");
        }

        var reference = now ?? DateTimeOffset.UtcNow;
        var baseTime = match.Groups[1].Value == "today"
            ? new DateTimeOffset(reference.UtcDateTime.Date, TimeSpan.Zero)
            : reference;
        if (!match.Groups[2].Success)
        {
            return baseTime;
        }

        var amount = int.Parse(match.Groups[2].Value);
        var unit = match.Groups[3].Success ? match.Groups[3].Value : "d";
        return unit == "h" ? baseTime.AddHours(amount) : baseTime.AddDays(amount);
    }

    public static bool TryResolve(string? expression, out DateTimeOffset value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        try
        {
            value = Resolve(expression);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
