namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed record CalendarEventViewResponse(
    string CalendarEventId,
    CalendarEventStart Start,
    DateTimeOffset ScheduledStartUtc,
    IReadOnlyList<LocalizedCalendarEventText> Descriptions,
    string Status,
    bool CanPublish,
    bool CanUpdate,
    bool CanDelete);
