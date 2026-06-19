using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Single calendar event read model used by the publish and delete use cases.
/// Carries the stored UTC start instant directly so the future-start guard and
/// broadcast scheduling do not re-derive it from local time and time zone, and
/// the stored <see cref="YouTubeBroadcastId"/> so the delete use case can remove
/// the external broadcast before the local row. The broadcast id is null until
/// the event has been published.
/// </summary>
public sealed record CalendarEventDetail(
    string CalendarEventId,
    DateTimeOffset ScheduledStartUtc,
    IReadOnlyList<LocalizedDescription> Descriptions,
    CalendarEventStatus Status,
    string? YouTubeBroadcastId = null);
