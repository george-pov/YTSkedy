namespace YTSkedy.AzureFunctions.Settings;

public sealed record StartDefaultsResponse(
    string? DayOfWeek,
    string? LocalTime,
    string? TimeZoneId);
