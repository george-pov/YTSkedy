namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record CalendarEvent(
    ScheduledStart Start,
    EventTextSnapshot Text);
