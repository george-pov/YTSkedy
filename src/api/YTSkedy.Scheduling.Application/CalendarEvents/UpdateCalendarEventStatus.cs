namespace YTSkedy.Scheduling.Application.CalendarEvents;

public enum UpdateCalendarEventStatus
{
    Updated,
    NotFound,
    HasPlatformPublications,
    Invalid,
    DuplicateScheduledStart,
    Conflict
}
