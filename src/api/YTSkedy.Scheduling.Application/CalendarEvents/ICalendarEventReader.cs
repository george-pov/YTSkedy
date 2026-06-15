namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventReader
{
    /// <summary>
    /// Reads candidate calendar event list items. When <paramref name="criteria"/>
    /// is supplied the read is scoped to that local calendar month; when it is
    /// null all stored events are returned. Sorting and paging are applied by the
    /// caller, so the returned order is not significant.
    /// </summary>
    Task<IReadOnlyList<CalendarEventListItem>> ListAsync(
        CalendarEventMonthCriteria? criteria,
        CancellationToken cancellationToken);

    Task<CalendarEventDetail?> GetByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads a single calendar event read model by id, or null when no event
    /// has the id. Carries the wall-clock local start and time zone, unlike
    /// <see cref="GetByIdAsync"/> which carries the UTC instant for publishing,
    /// so the edit UI can repopulate its form from stored local time.
    /// </summary>
    Task<CalendarEventListItem?> GetListItemByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken);
}
