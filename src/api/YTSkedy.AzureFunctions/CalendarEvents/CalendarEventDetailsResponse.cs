namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Response for <c>GET /api/calendar-events/{calendarEventId}</c>. Extends the
/// calendar event view shape with <c>platforms</c>: one publication item per
/// active platform plus orphan history, so a client can render the event details
/// (including publish status) from one read. This is the only endpoint that
/// exposes per-event publication state; the calendar event list endpoint stays
/// provider-neutral and does not carry this field.
/// </summary>
internal sealed record CalendarEventDetailsResponse(
    string CalendarEventId,
    CalendarEventStart Start,
    DateTimeOffset ScheduledStartUtc,
    string DisplayTitle,
    IReadOnlyList<EventTextResponse> Texts,
    IReadOnlyList<EventPlatformResponse> Platforms);
