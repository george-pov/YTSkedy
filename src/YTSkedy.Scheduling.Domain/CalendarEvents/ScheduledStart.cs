namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record ScheduledStart(
    DateTime LocalDateTime,
    string TimeZoneId);
