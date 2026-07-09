using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public sealed class DeleteThumbnailHandler(
    ICalendarEventReader calendarEventReader,
    IPlatformPublicationReader publicationReader,
    ICalendarEventThumbnailReader thumbnailReader,
    ICalendarEventThumbnailModifier thumbnailModifier,
    IThumbnailStore thumbnailStore)
{
    public async Task<DeleteThumbnailResult> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var calendarEvent = await calendarEventReader.GetByIdAsync(
            calendarEventId,
            cancellationToken);
        if (calendarEvent is null)
        {
            return DeleteThumbnailResult.EventNotFound;
        }

        if (await publicationReader.HasAnyForEventAsync(
                calendarEventId,
                cancellationToken))
        {
            return DeleteThumbnailResult.HasPlatformPublications;
        }

        var thumbnail = await thumbnailReader.GetThumbnailAsync(
            calendarEventId,
            cancellationToken);
        if (thumbnail is null)
        {
            return DeleteThumbnailResult.ThumbnailNotFound;
        }

        var deleted = await thumbnailModifier.DeleteThumbnailAsync(
            calendarEventId,
            cancellationToken);
        if (!deleted)
        {
            return DeleteThumbnailResult.EventNotFound;
        }

        await thumbnailStore.DeleteAsync(thumbnail.BlobName, cancellationToken);

        return DeleteThumbnailResult.Deleted;
    }
}
