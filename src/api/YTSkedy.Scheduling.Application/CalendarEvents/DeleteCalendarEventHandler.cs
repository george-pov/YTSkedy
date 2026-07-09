using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

public sealed class DeleteCalendarEventHandler(
    ICalendarEventReader calendarEventReader,
    IPlatformPublicationReader publicationReader,
    ICalendarEventModifier calendarEventModifier,
    ICalendarEventThumbnailReader thumbnailReader,
    IThumbnailStore thumbnailStore)
{
    public async Task<DeleteCalendarEventResult> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var calendarEvent = await calendarEventReader.GetByIdAsync(
            calendarEventId,
            cancellationToken);

        if (calendarEvent is null)
        {
            return DeleteCalendarEventResult.NotFound;
        }

        if (await publicationReader.HasAnyForEventAsync(
                calendarEventId,
                cancellationToken))
        {
            return DeleteCalendarEventResult.HasPlatformPublications;
        }

        var thumbnail = await thumbnailReader.GetThumbnailAsync(
            calendarEventId,
            cancellationToken);

        await calendarEventModifier.DeleteAsync(calendarEventId, cancellationToken);

        if (thumbnail is not null)
        {
            await DeleteThumbnailBytesBestEffortAsync(
                thumbnail.BlobName,
                cancellationToken);
        }

        return DeleteCalendarEventResult.Deleted;
    }

    private async Task DeleteThumbnailBytesBestEffortAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        try
        {
            await thumbnailStore.DeleteAsync(blobName, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Thumbnail bytes are secondary local artifacts. Event deletion is
            // already complete, so cleanup failures do not change the result.
        }
    }
}
