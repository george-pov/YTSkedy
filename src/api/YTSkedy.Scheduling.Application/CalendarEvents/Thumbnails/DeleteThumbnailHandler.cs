using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public sealed class DeleteThumbnailHandler(
    ICalendarEventReader calendarEventReader,
    CalendarEventPublicationLock publicationLock,
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

        if (await publicationLock.IsLockedAsync(
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

        var changeResult = await thumbnailModifier.DeleteThumbnailAsync(
            calendarEventId,
            cancellationToken);
        if (changeResult == CalendarEventChangeResult.NotFound)
        {
            return DeleteThumbnailResult.EventNotFound;
        }
        if (changeResult == CalendarEventChangeResult.Conflict)
        {
            return DeleteThumbnailResult.Conflict;
        }
        if (changeResult != CalendarEventChangeResult.Applied)
        {
            throw new InvalidOperationException(
                $"Unknown calendar event change result '{changeResult}'.");
        }

        await thumbnailStore.DeleteAsync(thumbnail.BlobName, cancellationToken);

        return DeleteThumbnailResult.Deleted;
    }
}
