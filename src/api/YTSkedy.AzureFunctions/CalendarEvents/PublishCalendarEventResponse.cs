namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed record PublishCalendarEventResponse(
    string CalendarEventId,
    string Status,
    string YouTubeBroadcastId);
