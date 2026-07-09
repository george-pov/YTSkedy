using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

public sealed class UploadThumbnailHandler(
    ICalendarEventReader calendarEventReader,
    IPlatformPublicationReader publicationReader,
    ICalendarEventThumbnailModifier thumbnailModifier,
    IThumbnailStore thumbnailStore,
    TimeProvider timeProvider)
{
    public async Task<UploadThumbnailResult> HandleAsync(
        UploadThumbnailCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.CalendarEventId);

        var calendarEvent = await calendarEventReader.GetByIdAsync(
            command.CalendarEventId,
            cancellationToken);
        if (calendarEvent is null)
        {
            return UploadThumbnailResult.EventNotFound;
        }

        if (await publicationReader.HasAnyForEventAsync(
                command.CalendarEventId,
                cancellationToken))
        {
            return UploadThumbnailResult.HasPlatformPublications;
        }

        var validation = ThumbnailValidator.Validate(
            command.FileName,
            command.ContentType,
            command.Content);
        if (!validation.IsValid)
        {
            return UploadThumbnailResult.Invalid(validation.Error!.Value);
        }

        var sanitizedFileName = ThumbnailValidator.SanitizeFileName(command.FileName);
        var blobName = BlobNameFor(command.CalendarEventId);
        var thumbnail = new Thumbnail(
            sanitizedFileName,
            command.ContentType,
            command.Content.LongLength,
            validation.Width,
            validation.Height,
            timeProvider.GetUtcNow(),
            blobName);

        await thumbnailStore.SaveAsync(
            blobName,
            command.Content,
            command.ContentType,
            cancellationToken);

        var saved = await thumbnailModifier.SaveThumbnailAsync(
            command.CalendarEventId,
            thumbnail,
            cancellationToken);

        return saved
            ? UploadThumbnailResult.Uploaded(thumbnail)
            : UploadThumbnailResult.EventNotFound;
    }

    private static string BlobNameFor(string calendarEventId) =>
        $"calendar-events/{calendarEventId}/thumbnail";
}
