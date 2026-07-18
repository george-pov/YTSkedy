using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public interface ICalendarEventThumbnailModifier
{
    /// <summary>
    /// Saves thumbnail metadata. A conflict means the row changed after the
    /// repository read and the caller must rerun use-case guards.
    /// </summary>
    Task<CalendarEventChangeResult> SaveThumbnailAsync(
        string calendarEventId,
        Thumbnail thumbnail,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes thumbnail metadata. A conflict means the row changed after the
    /// repository read and the caller must rerun use-case guards.
    /// </summary>
    Task<CalendarEventChangeResult> DeleteThumbnailAsync(
        string calendarEventId,
        CancellationToken cancellationToken);
}
