using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Settings;

public sealed record UpdateCalendarEventDefaultsCommand(
    IReadOnlyCollection<EventTextField> Fields,
    DayOfWeek? DayOfWeek,
    TimeOnly? LocalTime,
    string? TimeZoneId);
