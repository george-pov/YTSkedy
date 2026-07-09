using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public sealed class GetThumbnailHandler(
    ICalendarEventReader calendarEventReader,
    ICalendarEventThumbnailReader thumbnailReader,
    IThumbnailStore thumbnailStore)
{
    public async Task<GetThumbnailResult> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var calendarEvent = await calendarEventReader.GetByIdAsync(
            calendarEventId,
            cancellationToken);
        if (calendarEvent is null)
        {
            return GetThumbnailResult.EventNotFound;
        }

        var thumbnail = await thumbnailReader.GetThumbnailAsync(
            calendarEventId,
            cancellationToken);
        if (thumbnail is null)
        {
            return GetThumbnailResult.ThumbnailNotFound;
        }

        var content = await thumbnailStore.GetAsync(
            thumbnail.BlobName,
            cancellationToken);

        return content is null
            ? GetThumbnailResult.ThumbnailNotFound
            : GetThumbnailResult.Found(content);
    }
}
