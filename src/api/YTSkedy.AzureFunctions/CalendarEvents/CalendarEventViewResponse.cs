namespace YTSkedy.AzureFunctions.CalendarEvents;

internal sealed record CalendarEventViewResponse(
    string CalendarEventId,
    CalendarEventStart Start,
    DateTimeOffset ScheduledStartUtc,
    string DisplayTitle,
    IReadOnlyList<EventTextResponse> Texts);
