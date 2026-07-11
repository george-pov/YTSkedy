namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Field a calendar event list query sorts on. Mirrors the columns the UI can
/// sort. Scheduled start sorts on the calendar event id, which equals the UTC
/// start instant order.
/// </summary>
public enum CalendarEventSortField
{
    ScheduledStart = 0,
    TimeZone = 1,
    Title = 2,
    PublicationStatus = 3
}
