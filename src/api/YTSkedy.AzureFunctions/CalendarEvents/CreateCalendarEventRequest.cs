namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record CreateCalendarEventRequest(
    CalendarEventStart Start,
    IReadOnlyList<EventTextRequest> Texts);

internal sealed record CalendarEventStart(
    DateTime LocalDateTime,
    string TimeZoneId);

internal sealed record EventTextRequest(
    string FieldKey,
    string Value);
