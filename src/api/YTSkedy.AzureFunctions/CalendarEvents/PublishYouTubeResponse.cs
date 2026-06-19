namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed record PublishYouTubeResponse(
    string CalendarEventId,
    string Status,
    string YouTubeBroadcastId);
