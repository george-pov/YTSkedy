using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public interface ICalendarEventThumbnailModifier
{
    Task<bool> SaveThumbnailAsync(
        string calendarEventId,
        Thumbnail thumbnail,
        CancellationToken cancellationToken);

    Task<bool> DeleteThumbnailAsync(
        string calendarEventId,
        CancellationToken cancellationToken);
}
