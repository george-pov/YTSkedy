using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Applies server-side sorting and paging over the candidate calendar events
/// returned by the reader. Sort, paging, and total-count policy live here so the
/// behavior is testable without storage. Every sort uses the calendar event id
/// ascending as a deterministic secondary key, so paging stays stable when the
/// primary field ties.
/// </summary>
public sealed class ListEventsHandler(ICalendarEventReader calendarEvents)
{
    public async Task<CalendarEventListPage> HandleAsync(
        CalendarEventListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var criteria = query.Year.HasValue && query.Month.HasValue
            ? new CalendarEventMonthCriteria(query.Year.Value, query.Month.Value)
            : null;

        var candidates = await calendarEvents.ListAsync(criteria, cancellationToken);

        var items = Sort(candidates, query.Sort, query.Direction)
            .Skip(query.Page * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new CalendarEventListPage(
            items,
            query.Page,
            query.PageSize,
            candidates.Count,
            query.Sort,
            query.Direction);
    }

    private static IEnumerable<CalendarEventView> Sort(
        IReadOnlyList<CalendarEventView> candidates,
        CalendarEventSortField sort,
        SortDirection direction)
    {
        var ordered = sort switch
        {
            CalendarEventSortField.TimeZone => direction == SortDirection.Descending
                ? candidates.OrderByDescending(item => item.Start.TimeZoneId, StringComparer.Ordinal)
                : candidates.OrderBy(item => item.Start.TimeZoneId, StringComparer.Ordinal),
            CalendarEventSortField.Title => direction == SortDirection.Descending
                ? candidates.OrderByDescending(item => item.Text.DisplayTitle, StringComparer.Ordinal)
                : candidates.OrderBy(item => item.Text.DisplayTitle, StringComparer.Ordinal),
            _ => direction == SortDirection.Descending
                ? candidates.OrderByDescending(item => item.ScheduledStartUtc)
                : candidates.OrderBy(item => item.ScheduledStartUtc)
        };

        return ordered.ThenBy(item => item.CalendarEventId, StringComparer.Ordinal);
    }
}
