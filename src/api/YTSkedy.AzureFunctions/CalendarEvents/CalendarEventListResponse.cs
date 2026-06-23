namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Paged envelope returned by <c>GET /api/calendar-events</c>. Carries the page
/// items plus the paging metadata and the echoed sort the UI paginator needs.
/// </summary>
internal sealed record CalendarEventListResponse(
    IReadOnlyList<CalendarEventViewResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    string Sort,
    string Direction);
