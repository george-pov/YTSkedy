using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventRepository
{
    Task<string> CreateAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken);
}
