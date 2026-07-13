namespace YTSkedy.Scheduling.Application.CalendarEvents.Starts;

public sealed record DefaultStart(
    DateOnly? LocalDate,
    TimeOnly? LocalTime,
    string? TimeZoneId);
