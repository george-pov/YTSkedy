namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record CreateCalendarEventRequest(
    CalendarEventStart Start,
    IReadOnlyList<LocalizedCalendarEventText> Descriptions);

internal sealed record CalendarEventStart(
    DateTime LocalDateTime,
    string TimeZoneId);

internal sealed record LocalizedCalendarEventText(
    string Language,
    string Title,
    string? Description);
