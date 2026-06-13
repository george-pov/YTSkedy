using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record CalendarEventListItem(
    string CalendarEventId,
    ScheduledStart Start,
    IReadOnlyList<LocalizedDescription> Descriptions,
    CalendarEventStatus Status);
