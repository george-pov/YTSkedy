using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;

public sealed class PublicationThumbnailApplier(
    ICalendarEventThumbnailReader thumbnails,
    IThumbnailStore thumbnailStore,
    IPublicationThumbnailWriter publicationThumbnails,
    IPlatformTypeAdapterSelector<IThumbnailPublisher> thumbnailPublishers,
    ILogger<PublicationThumbnailApplier> logger)
{
    public async Task<PublicationThumbnail> LoadAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var thumbnail = await thumbnails.GetThumbnailAsync(calendarEventId, cancellationToken);
        if (thumbnail is null)
        {
            return PublicationThumbnail.NotConfigured;
        }

        var content = await thumbnailStore.GetAsync(thumbnail.BlobName, cancellationToken);

        return content is null
            ? PublicationThumbnail.MissingContent
            : PublicationThumbnail.Configured(content);
    }

    public async Task<ThumbnailPublishStatus?> ApplyAsync(
        PublicationThumbnailCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Platform);
        ArgumentNullException.ThrowIfNull(command.Thumbnail);

        var thumbnailStatus = ThumbnailPublicationPolicy.InitialStatusFor(command.Platform.Type);
        if (thumbnailStatus is null)
        {
            return null;
        }

        if (!command.Thumbnail.IsConfigured)
        {
            return ThumbnailPublishStatus.NotConfigured;
        }

        if (command.Thumbnail.Content is null)
        {
            await MarkThumbnailFailedAsync(
                command.CalendarEventId,
                command.PlatformId,
                "stored thumbnail bytes were not found",
                cancellationToken);

            return ThumbnailPublishStatus.Failed;
        }

        var thumbnailPublisher = thumbnailPublishers.Find(command.Platform.Type);
        if (thumbnailPublisher is null)
        {
            await MarkThumbnailFailedAsync(
                command.CalendarEventId,
                command.PlatformId,
                "no thumbnail publisher was registered for the platform type",
                cancellationToken);

            return ThumbnailPublishStatus.Failed;
        }

        try
        {
            await thumbnailPublisher.PublishAsync(
                new ThumbnailPublishRequest(
                    command.CalendarEventId,
                    command.PlatformId,
                    command.ExternalResourceId,
                    command.Platform.PublishSettings,
                    command.Thumbnail.Content),
                cancellationToken);
        }
        catch (ThumbnailPublishException exception)
        {
            logger.LogWarning(
                exception,
                "Thumbnail application failed for calendar event {CalendarEventId}, platform " +
                "{PlatformId}, and external resource {ExternalResourceId}.",
                command.CalendarEventId,
                command.PlatformId,
                command.ExternalResourceId);

            await MarkThumbnailFailedAsync(
                command.CalendarEventId,
                command.PlatformId,
                "provider thumbnail upload failed",
                cancellationToken);

            return ThumbnailPublishStatus.Failed;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Thumbnail application failed unexpectedly for calendar event {CalendarEventId}, " +
                "platform {PlatformId}, and external resource {ExternalResourceId}.",
                command.CalendarEventId,
                command.PlatformId,
                command.ExternalResourceId);

            await MarkThumbnailFailedAsync(
                command.CalendarEventId,
                command.PlatformId,
                "provider thumbnail upload failed unexpectedly",
                cancellationToken);

            return ThumbnailPublishStatus.Failed;
        }

        var recorded = await publicationThumbnails.MarkThumbnailAppliedAsync(
            command.CalendarEventId,
            command.PlatformId,
            cancellationToken);
        if (!recorded)
        {
            logger.LogWarning(
                "Thumbnail application succeeded for calendar event {CalendarEventId} and platform " +
                "{PlatformId}, but the publication row no longer accepted the thumbnail status update.",
                command.CalendarEventId,
                command.PlatformId);
        }

        return ThumbnailPublishStatus.Applied;
    }

    private async Task MarkThumbnailFailedAsync(
        string calendarEventId,
        string platformId,
        string reason,
        CancellationToken cancellationToken)
    {
        var recorded = await publicationThumbnails.MarkThumbnailFailedAsync(
            calendarEventId,
            platformId,
            cancellationToken);
        if (!recorded)
        {
            logger.LogWarning(
                "Thumbnail application failed for calendar event {CalendarEventId} and platform " +
                "{PlatformId} because {Reason}, but the publication row no longer accepted the " +
                "thumbnail status update.",
                calendarEventId,
                platformId,
                reason);
        }
    }
}
