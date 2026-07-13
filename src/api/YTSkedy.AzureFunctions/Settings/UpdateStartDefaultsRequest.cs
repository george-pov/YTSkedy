namespace YTSkedy.AzureFunctions.Settings;

public sealed record UpdateStartDefaultsRequest(
    string? DayOfWeek,
    string? LocalTime,
    string? TimeZoneId);
