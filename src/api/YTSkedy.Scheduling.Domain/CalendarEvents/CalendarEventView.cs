namespace YTSkedy.Scheduling.Domain.CalendarEvents;

public sealed record CalendarEventView(
    string CalendarEventId,
    ScheduledStart Start,
    DateTimeOffset ScheduledStartUtc,
    EventTextSnapshot Text);
