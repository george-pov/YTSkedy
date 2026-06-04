namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed record CreateCalendarEventRequest(
    CalendarEventStart Start,
    IReadOnlyList<LocalizedCalendarEventText> Descriptions);

public sealed record CalendarEventStart(
    DateTime LocalDateTime,
    string TimeZoneId);

public sealed record LocalizedCalendarEventText(
    string Language,
    string Title,
    string? Description);
