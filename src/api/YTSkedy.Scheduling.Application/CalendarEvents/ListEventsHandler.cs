using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Applies server-side sorting and paging over the candidate calendar events
/// returned by the reader. Sort, paging, and total-count policy live here so the
/// behavior is testable without storage. Every sort uses the calendar event id
/// ascending as a deterministic secondary key, so paging stays stable when the
/// primary field ties.
/// </summary>
public sealed class ListEventsHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms)
{
    public async Task<CalendarEventListPage> HandleAsync(
        CalendarEventListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var criteria = query.Year.HasValue && query.Month.HasValue
            ? new CalendarEventMonthCriteria(query.Year.Value, query.Month.Value)
            : null;

        var records = await calendarEvents.ListAsync(criteria, cancellationToken);
        var activePlatformIds = await platforms.ListIdsAsync(cancellationToken);
        var candidates = records
            .Select(record => new CalendarEventListItem(
                record.Event,
                PublishingStatusMapper.Map(
                    record.PublishedPlatformIds,
                    activePlatformIds)))
            .ToArray();

        var items = Sort(candidates, query.Sort, query.Direction)
            .Skip(query.Page * query.PageSize)
            .Take(query.PageSize)
            .ToArray();

        return new CalendarEventListPage(
            items,
            query.Page,
            query.PageSize,
            records.Count,
            query.Sort,
            query.Direction);
    }

    private static IEnumerable<CalendarEventListItem> Sort(
        IReadOnlyList<CalendarEventListItem> candidates,
        CalendarEventSortField sort,
        SortDirection direction)
    {
        var ordered = sort switch
        {
            CalendarEventSortField.TimeZone => direction == SortDirection.Descending
                ? candidates.OrderByDescending(
                    item => item.Event.Start.TimeZoneId,
                    StringComparer.Ordinal)
                : candidates.OrderBy(
                    item => item.Event.Start.TimeZoneId,
                    StringComparer.Ordinal),
            CalendarEventSortField.Title => direction == SortDirection.Descending
                ? candidates.OrderByDescending(
                    item => item.Event.Text.DisplayTitle,
                    StringComparer.Ordinal)
                : candidates.OrderBy(
                    item => item.Event.Text.DisplayTitle,
                    StringComparer.Ordinal),
            CalendarEventSortField.PublicationStatus =>
                direction == SortDirection.Descending
                    ? candidates.OrderByDescending(item => item.PublicationStatus)
                    : candidates.OrderBy(item => item.PublicationStatus),
            _ => direction == SortDirection.Descending
                ? candidates.OrderByDescending(item => item.Event.ScheduledStartUtc)
                : candidates.OrderBy(item => item.Event.ScheduledStartUtc)
        };

        return ordered.ThenBy(
            item => item.Event.CalendarEventId,
            StringComparer.Ordinal);
    }
}
