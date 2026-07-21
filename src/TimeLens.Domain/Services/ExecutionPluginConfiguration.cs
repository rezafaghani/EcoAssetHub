using System.Text.Json;
using System.Xml;

namespace TimeLens.Domain.Services;

public static class ExecutionPluginConfiguration
{
    public static TimeSpan? GetOptionalDuration(JsonElement configuration, params string[] names)
    {
        foreach (var name in names)
        {
            if (configuration.ValueKind == JsonValueKind.Object
                && configuration.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
                && TryParseDuration(value.GetString(), out var duration))
            {
                return duration;
            }
        }

        return null;
    }

    public static bool TryGetDuration(JsonElement configuration, string name, out TimeSpan duration)
    {
        var parsed = GetOptionalDuration(configuration, name);
        duration = parsed ?? default;
        return parsed is not null;
    }

    public static double? GetOptionalDouble(JsonElement configuration, params string[] names)
    {
        foreach (var name in names)
        {
            if (configuration.ValueKind != JsonValueKind.Object || !configuration.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            {
                return number;
            }
        }

        return null;
    }

    public static int? GetOptionalInt(JsonElement configuration, params string[] names)
    {
        foreach (var name in names)
        {
            if (configuration.ValueKind == JsonValueKind.Object
                && configuration.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetInt32(out var number))
            {
                return number;
            }
        }

        return null;
    }

    public static bool TryParseDuration(string? value, out TimeSpan duration)
    {
        duration = value?.Trim().ToLowerInvariant() switch
        {
            "quarter-hour" or "quarterhour" or "15min" or "15m" => TimeSpan.FromMinutes(15),
            "hour" or "hourly" or "1h" => TimeSpan.FromHours(1),
            "day" or "daily" or "1d" => TimeSpan.FromDays(1),
            _ => default
        };
        if (duration > TimeSpan.Zero)
        {
            return true;
        }

        try
        {
            duration = string.IsNullOrWhiteSpace(value) ? default : XmlConvert.ToTimeSpan(value);
            return duration > TimeSpan.Zero;
        }
        catch (FormatException)
        {
            duration = default;
            return false;
        }
    }
}
