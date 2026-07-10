namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface IPublicationIndexWriter
{
    Task<bool> AddPublishedPlatformAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken);

    Task<bool> RemovePublishedPlatformAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken);
}
