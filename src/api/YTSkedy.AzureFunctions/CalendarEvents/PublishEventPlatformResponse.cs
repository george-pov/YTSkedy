namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// Success body for
/// <c>POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish</c>.
/// Echoes the event and platform identity, the resulting publication status, and
/// the recorded external resource id and publish instant.
/// </summary>
public sealed record PublishEventPlatformResponse(
    string CalendarEventId,
    string PlatformId,
    string PlatformName,
    string PlatformType,
    string Status,
    string ExternalResourceId,
    DateTimeOffset PublishedUtc);
