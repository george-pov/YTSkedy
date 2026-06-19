namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Result of the update calendar event use case as seen by the HTTP host.
/// <c>Updated</c> maps to 200, <c>NotFound</c> to 404, and <c>NotUpdatable</c>
/// to 409 because the event is no longer a Draft and its descriptions are frozen
/// against the metadata already published to YouTube.
/// </summary>
public enum UpdateCalendarEventResult
{
    Updated,
    NotFound,
    NotUpdatable
}
