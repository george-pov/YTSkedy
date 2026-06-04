using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record CreateCalendarEventCommand(
    ScheduledStart Start,
    IReadOnlyList<LocalizedDescription> Descriptions);
