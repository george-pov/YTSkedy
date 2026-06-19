namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Result of the delete calendar event use case as seen by the HTTP host.
/// <c>Deleted</c> maps to 204, <c>NotFound</c> to 404, <c>NotDeletable</c> to
/// 409 because the event is not deletable in its current state (Publishing, a
/// past Published event, or a Draft that was concurrently advanced),
/// <c>MissingYouTubeBroadcastId</c> to 409 because a future Published event has
/// no recorded broadcast id to delete and the local row was kept, and
/// <c>YouTubeDeleteFailed</c> to 502 because the YouTube broadcast could not be
/// deleted and the local row was kept.
/// </summary>
public enum DeleteCalendarEventResult
{
    Deleted,
    NotFound,
    NotDeletable,
    MissingYouTubeBroadcastId,
    YouTubeDeleteFailed
}
