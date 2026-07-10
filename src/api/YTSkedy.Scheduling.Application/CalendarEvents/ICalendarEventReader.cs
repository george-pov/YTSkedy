using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventReader
{
    /// <summary>
    /// Reads candidate calendar event views. When <paramref name="criteria"/>
    /// is supplied the read is scoped to that local calendar month; when it is
    /// null all stored events are returned. Sorting and paging are applied by the
    /// caller, so the returned order is not significant.
    /// </summary>
    Task<IReadOnlyList<CalendarEventListRecord>> ListAsync(
        CalendarEventMonthCriteria? criteria,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads a single calendar event view by id, or null when no event has the
    /// id. Carries the wall-clock local start and time zone alongside the UTC
    /// instant, so the publish, delete, and edit use cases all read one model.
    /// </summary>
    Task<CalendarEventView?> GetByIdAsync(
        string calendarEventId,
        CancellationToken cancellationToken);
}
