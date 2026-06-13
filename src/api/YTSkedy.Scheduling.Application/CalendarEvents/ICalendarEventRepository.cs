using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventRepository
{
    Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken);

    Task UpdateStatusAsync(
        string calendarEventId,
        CalendarEventStatus status,
        string youTubeBroadcastId,
        CancellationToken cancellationToken);
}
