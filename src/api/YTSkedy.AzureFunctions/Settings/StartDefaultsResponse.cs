namespace YTSkedy.AzureFunctions.Settings;

internal sealed record StartDefaultsResponse(
    string? DayOfWeek,
    string? LocalTime,
    string? TimeZoneId);
