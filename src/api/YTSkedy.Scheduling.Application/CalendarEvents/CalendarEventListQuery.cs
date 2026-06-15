namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Server-side paged and sorted calendar event list query. <see cref="Year"/>
/// and <see cref="Month"/> are optional; when both are set the read is scoped to
/// that local calendar month, otherwise all events are candidates.
/// </summary>
public sealed record CalendarEventListQuery(
    int Page,
    int PageSize,
    CalendarEventSortField Sort,
    CalendarEventSortDirection Direction,
    int? Year,
    int? Month);
