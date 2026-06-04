namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventReader
{
    Task<IReadOnlyList<CalendarEventListItem>> ListByMonthAsync(
        CalendarEventMonthCriteria criteria,
        CancellationToken cancellationToken);
}
