using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public sealed record UpdateEventTextFieldsCommand(
    IReadOnlyList<EventTextField> Fields);
