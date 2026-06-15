namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Loads a single calendar event read model by id for the edit UI. Returns the
/// wall-clock local start, time zone, descriptions, and status so the edit form
/// can repopulate without re-deriving local time from the stored UTC instant.
/// Returns null when no event has the id.
/// </summary>
public sealed class GetCalendarEventHandler(ICalendarEventReader calendarEvents)
{
    public async Task<CalendarEventListItem?> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        return await calendarEvents.GetListItemByIdAsync(
            calendarEventId,
            cancellationToken);
    }
}
