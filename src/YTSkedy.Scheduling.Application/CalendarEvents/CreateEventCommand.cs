using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record CreateEventCommand(
    ScheduledStart Start,
    IReadOnlyList<LocalizedDescription> Descriptions);
