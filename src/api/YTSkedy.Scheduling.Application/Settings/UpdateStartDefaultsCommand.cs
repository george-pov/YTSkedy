namespace YTSkedy.Scheduling.Application.Settings;

public sealed record UpdateStartDefaultsCommand(
    DayOfWeek? DayOfWeek,
    TimeOnly? LocalTime,
    string? TimeZoneId);
