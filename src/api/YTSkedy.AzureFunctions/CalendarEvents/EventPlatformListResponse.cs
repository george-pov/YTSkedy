namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Envelope returned by <c>GET /api/calendar-events/{calendarEventId}/platforms</c>.
/// Echoes the calendar event id and lists one item per active registered
/// platform plus any orphaned publication history rows.
/// </summary>
public sealed record EventPlatformListResponse(
    string CalendarEventId,
    IReadOnlyList<EventPlatformResponse> Items);
