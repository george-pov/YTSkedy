namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Result of the id-only repository delete used for post-YouTube Published
/// cleanup. Unlike <see cref="DeleteDraftCalendarEventResult"/> this delete is
/// unconditional and never checks status, so it can only report that the row was
/// <c>Deleted</c> or that it was already <c>NotFound</c>. Both are
/// success-equivalent to the delete use case once YouTube cleanup has happened.
/// </summary>
public enum DeleteCalendarEventRowResult
{
    Deleted,
    NotFound
}
