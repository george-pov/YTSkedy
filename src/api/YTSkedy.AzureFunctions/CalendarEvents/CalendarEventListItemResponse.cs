namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed record CalendarEventListItemResponse(
    string CalendarEventId,
    CalendarEventStart Start,
    DateTimeOffset ScheduledStartUtc,
    IReadOnlyList<LocalizedCalendarEventText> Descriptions,
    string Status);
