namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record CalendarEventViewResponse(
    string CalendarEventId,
    CalendarEventStart Start,
    DateTimeOffset ScheduledStartUtc,
    IReadOnlyList<LocalizedCalendarEventText> Descriptions);
