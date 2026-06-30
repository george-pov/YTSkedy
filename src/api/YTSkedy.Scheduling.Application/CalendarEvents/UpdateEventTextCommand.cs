using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed record UpdateEventTextCommand(
    string CalendarEventId,
    IReadOnlyList<EventTextValue> Texts);
