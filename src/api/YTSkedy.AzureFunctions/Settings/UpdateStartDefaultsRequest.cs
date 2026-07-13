namespace YTSkedy.AzureFunctions.Settings;

internal sealed record UpdateStartDefaultsRequest(
    string? DayOfWeek,
    string? LocalTime,
    string? TimeZoneId);
