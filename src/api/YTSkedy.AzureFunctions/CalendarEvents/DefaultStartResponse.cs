namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed record DefaultStartResponse(
    string? LocalDate,
    string? LocalTime,
    string? TimeZoneId);
