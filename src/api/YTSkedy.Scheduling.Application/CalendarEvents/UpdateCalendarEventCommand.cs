using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record UpdateCalendarEventCommand(
    string CalendarEventId,
    ScheduledStart Start,
    IReadOnlyList<EventTextValue> Texts);
