namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// One page of a server-side sorted calendar event list. <see cref="TotalCount"/>
/// is the full candidate count across all pages, used to drive the paginator.
/// </summary>
public sealed record CalendarEventListPage(
    IReadOnlyList<CalendarEventListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    CalendarEventSortField Sort,
    CalendarEventSortDirection Direction);
