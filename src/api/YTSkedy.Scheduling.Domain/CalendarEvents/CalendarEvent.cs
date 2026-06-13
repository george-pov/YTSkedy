namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record CalendarEvent(
    ScheduledStart Start,
    IReadOnlyList<LocalizedDescription> Descriptions,
    CalendarEventStatus Status = CalendarEventStatus.Draft);
