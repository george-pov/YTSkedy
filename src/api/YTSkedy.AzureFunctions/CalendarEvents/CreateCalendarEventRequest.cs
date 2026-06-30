namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record CreateCalendarEventRequest(
    CalendarEventStart Start,
    IReadOnlyList<EventTextPayload> Texts);

internal sealed record CalendarEventStart(
    DateTime LocalDateTime,
    string TimeZoneId);

internal sealed record EventTextPayload(
    string FieldKey,
    string Value);
