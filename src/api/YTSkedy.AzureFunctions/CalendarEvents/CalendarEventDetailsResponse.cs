namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Response for <c>GET /api/calendar-events/{calendarEventId}</c>. Extends the
/// calendar event view shape with root event action flags and
/// <c>platforms</c>: one publication item per active platform plus orphan
/// history, so a client can render the event details and mutation eligibility
/// from one read. This is the only endpoint that exposes per-event publication
/// state; the calendar event list endpoint stays provider-neutral and does not
/// carry these fields.
/// </summary>
internal sealed record CalendarEventDetailsResponse(
    string CalendarEventId,
    CalendarEventStart Start,
    DateTimeOffset ScheduledStartUtc,
    string DisplayTitle,
    bool CanUpdate,
    bool CanDelete,
    IReadOnlyList<EventTextResponse> Texts,
    IReadOnlyList<EventPlatformResponse> Platforms);
