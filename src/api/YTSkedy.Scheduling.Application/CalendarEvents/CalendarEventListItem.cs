using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record CalendarEventListItem(
    string CalendarEventId,
    ScheduledStart Start,
    DateTimeOffset ScheduledStartUtc,
    IReadOnlyList<LocalizedDescription> Descriptions,
    CalendarEventStatus Status);
