using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Single calendar event read model used by the publish use case. Carries the
/// stored UTC start instant directly so the future-start guard and broadcast
/// scheduling do not re-derive it from local time and time zone.
/// </summary>
public sealed record CalendarEventDetail(
    string CalendarEventId,
    DateTimeOffset ScheduledStartUtc,
    IReadOnlyList<LocalizedDescription> Descriptions,
    CalendarEventStatus Status);
