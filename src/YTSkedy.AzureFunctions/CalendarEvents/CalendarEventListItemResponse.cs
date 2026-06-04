namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed record CalendarEventListItemResponse(
    string CalendarEventId,
    CalendarEventStart Start,
    IReadOnlyList<LocalizedCalendarEventText> Descriptions);
