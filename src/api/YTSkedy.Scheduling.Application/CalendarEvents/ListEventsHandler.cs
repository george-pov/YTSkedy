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
        var primaryKey = PrimaryKey(sort);

        var ordered = direction == SortDirection.Descending
            ? candidates.OrderByDescending(primaryKey, StringComparer.Ordinal)
            : candidates.OrderBy(primaryKey, StringComparer.Ordinal);

        return ordered.ThenBy(item => item.CalendarEventId, StringComparer.Ordinal);
    }

    private static Func<CalendarEventView, string> PrimaryKey(
        CalendarEventSortField sort) =>
        sort switch
        {
            CalendarEventSortField.Status => item => item.Status.ToString(),
            CalendarEventSortField.TimeZone => item => item.Start.TimeZoneId,
            CalendarEventSortField.Title => EnglishTitle,
            _ => item => item.CalendarEventId
        };

    // Sort key for the Title column: the English description title. English
    // matching is case-insensitive (see CalendarEventLanguages), so an entry
    // tagged "en", "EN", or "En" is used identically. Both languages are
    // required when an event is created, so the English entry is normally
    // present; a missing entry sorts as an empty string.
    private static string EnglishTitle(CalendarEventView item) =>
        item.Descriptions
            .FirstOrDefault(description => description.IsEnglish)
            ?.Title ?? string.Empty;
}
