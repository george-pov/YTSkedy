using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface ICalendarEventThumbnailReader
{
    Task<Thumbnail?> GetThumbnailAsync(
        string calendarEventId,
        CancellationToken cancellationToken);
}
