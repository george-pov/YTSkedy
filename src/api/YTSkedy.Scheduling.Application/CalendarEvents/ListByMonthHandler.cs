namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class ListByMonthHandler(ICalendarEventReader calendarEvents)
{
    public Task<IReadOnlyList<CalendarEventListItem>> HandleAsync(
        CalendarEventMonthCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        return calendarEvents.ListByMonthAsync(
            criteria,
            cancellationToken);
    }
}
