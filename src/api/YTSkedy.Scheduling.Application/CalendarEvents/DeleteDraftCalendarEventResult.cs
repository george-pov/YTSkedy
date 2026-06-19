namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Result of the repository-level conditional delete. Reported at the
/// persistence boundary so the handler does not see storage identity or ETags:
/// <c>Deleted</c> when the Draft row was removed, <c>NotFound</c> when the row
/// was already gone, and <c>NotDeletable</c> when the row was no longer Draft
/// or changed under a concurrent write.
/// </summary>
public enum DeleteDraftCalendarEventResult
{
    Deleted,
    NotFound,
    NotDeletable
}
