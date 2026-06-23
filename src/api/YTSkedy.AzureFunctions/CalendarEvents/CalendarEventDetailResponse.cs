namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Response for <c>GET /api/calendar-events/{calendarEventId}</c>. Extends the
/// calendar event view shape with <c>platforms</c>: one publication item per
/// active platform plus orphan history, so a client can render the event detail
/// (including publish status) from one read. This is the only endpoint that
/// exposes per-event publication state; the calendar event list endpoint stays
/// provider-neutral and does not carry this field.
/// </summary>
public sealed record CalendarEventDetailResponse(
    string CalendarEventId,
    CalendarEventStart Start,
    DateTimeOffset ScheduledStartUtc,
    IReadOnlyList<LocalizedCalendarEventText> Descriptions,
    IReadOnlyList<EventPlatformResponse> Platforms);
