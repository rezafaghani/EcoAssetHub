namespace EcoAssetHub.Infrastructure;

internal static class DbValue
{
    public static object From<T>(T? value) => value is null ? DBNull.Value : value;
    public static DateTime Utc(DateTimeOffset value) => value.UtcDateTime;
    public static DateTime? Utc(DateTimeOffset? value) => value?.UtcDateTime;
}
