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
}
