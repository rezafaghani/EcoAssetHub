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
