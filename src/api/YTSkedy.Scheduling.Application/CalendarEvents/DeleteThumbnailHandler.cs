using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

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

        var publicationRows = await publicationReader.ListByEventAsync(
            calendarEventId,
            cancellationToken);
        if (publicationRows.Count > 0)
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
