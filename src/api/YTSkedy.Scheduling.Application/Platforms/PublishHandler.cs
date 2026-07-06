using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Publishes one calendar event to one selected platform. The flow loads the
/// event, the platform, and the selected provider, then guards state and content:
/// an existing row that is orphaned, published, or publishing is a conflict; the
/// start must be in the future; and publishing content must render to a valid
/// title without unresolved placeholders. It then starts the publication row
/// with a content snapshot (a conditional write, so a concurrent
/// publish yields a conflict), calls the provider, and finalizes the row with the
/// external resource id. A provider failure releases the attempt and surfaces
/// an upstream failure; a finalize failure after the external resource was
/// created is recorded for follow-up.
/// </summary>
public sealed class PublishHandler(
    ICalendarEventReader calendarEvents,
    ICalendarEventThumbnailReader thumbnails,
    IThumbnailStore thumbnailStore,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    IPlatformPublicationRepository publicationRepository,
    IPlatformPublisherSelector publishers,
    IThumbnailPublisherSelector thumbnailPublishers,
    PublishingContentRenderer contentRenderer,
    TimeProvider timeProvider,
    ILogger<PublishHandler> logger)
{
    public async Task<PublishResult> HandleAsync(
        PublishCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await calendarEvents.GetByIdAsync(
            command.CalendarEventId,
            cancellationToken);
        if (calendarEvent is null)
        {
            return PublishResult.ForStatus(PublishResultStatus.EventNotFound);
        }

        var platform = await platforms.GetAsync(command.PlatformId, cancellationToken);
        if (platform is null)
        {
            return PublishResult.ForStatus(PublishResultStatus.PlatformNotFound);
        }

        // Selecting the provider before starting avoids leaving a Publishing row
        // stranded when no adapter serves the platform type.
        var publisher = publishers.Find(platform.Type);
        if (publisher is null)
        {
            return PublishResult.ForStatus(PublishResultStatus.ProviderNotSupported);
        }

        var existing = await publications.GetAsync(
            command.CalendarEventId,
            command.PlatformId,
            cancellationToken);
        if (existing is not null)
        {
            // A NotPublished pair has no row. Orphaned history cannot be republished.
            if (existing.IsOrphaned)
            {
                return PublishResult.ForStatus(PublishResultStatus.PlatformDeleted);
            }

            if (existing.Status == PublishStatus.Published)
            {
                return PublishResult.ForStatus(PublishResultStatus.AlreadyPublished);
            }

            if (existing.Status == PublishStatus.Publishing)
            {
                return PublishResult.ForStatus(PublishResultStatus.PublishInProgress);
            }
        }

        if (calendarEvent.ScheduledStartUtc <= timeProvider.GetUtcNow())
        {
            return PublishResult.ForStatus(PublishResultStatus.PastStart);
        }

        var runtimeTokenValues = await PlatformReferenceTokenValues.BuildAsync(
            platforms,
            publications,
            command.CalendarEventId,
            cancellationToken);

        var renderResult = await contentRenderer.RenderAsync(
            platform,
            calendarEvent,
            runtimeTokenValues,
            cancellationToken);
        if (renderResult.Status != RenderContentStatus.Rendered ||
            renderResult.HasUnresolvedPlaceholders)
        {
            return PublishResult.ForStatus(PublishResultStatus.InvalidPublishingContent);
        }

        var renderedContent = renderResult.Content!;
        var contentSnapshot = new ContentSnapshot(
            renderedContent.Title,
            renderedContent.Description);

        var attempt = new PlatformPublicationAttempt(
            command.CalendarEventId,
            command.PlatformId,
            platform.Name,
            platform.Type,
            platform.PublishSettings,
            contentSnapshot);

        var startResult = await publicationRepository.StartPublishingAsync(
            attempt,
            cancellationToken);
        if (startResult == StartPublicationResult.Conflict)
        {
            // A concurrent publish won the conditional start write.
            return PublishResult.ForStatus(PublishResultStatus.PublishInProgress);
        }

        PublishThumbnail thumbnail;
        try
        {
            thumbnail = await LoadThumbnailAsync(command.CalendarEventId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Failed to load thumbnail content for calendar event {CalendarEventId} before " +
                "publishing platform {PlatformId}.",
                command.CalendarEventId,
                command.PlatformId);

            await publicationRepository.ReleasePublishingAsync(
                command.CalendarEventId,
                command.PlatformId,
                cancellationToken);

            return PublishResult.ForStatus(PublishResultStatus.ProviderFailed);
        }

        PlatformPublishResult publishResult;
        try
        {
            publishResult = await publisher.PublishAsync(
                new PlatformPublishRequest(
                    command.CalendarEventId,
                    command.PlatformId,
                    platform.PublishSettings,
                    renderedContent.Title,
                    renderedContent.Description,
                    calendarEvent.ScheduledStartUtc),
                cancellationToken);
        }
        catch (PlatformPublishException exception)
        {
            // Release the attempt so the pair returns to NotPublished and can
            // be retried. The provider already logged secret-safe details.
            logger.LogError(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} failed.",
                command.CalendarEventId,
                command.PlatformId);

            await publicationRepository.ReleasePublishingAsync(
                command.CalendarEventId,
                command.PlatformId,
                cancellationToken);

            return PublishResult.ForStatus(PublishResultStatus.ProviderFailed);
        }

        var publishedUtc = await publicationRepository.MarkPublishedAsync(
            command.CalendarEventId,
            command.PlatformId,
            publishResult.ExternalResourceId,
            cancellationToken);
        if (publishedUtc is null)
        {
            // The external resource exists but the started row vanished before it
            // could be finalized. The publish finalization path does not delete
            // provider resources, so record the external resource for follow-up.
            logger.LogError(
                "Published calendar event {CalendarEventId} to platform {PlatformId} as external " +
                "resource {ExternalResourceId}, but the publication row could not be finalized.",
                command.CalendarEventId,
                command.PlatformId,
                publishResult.ExternalResourceId);

            return PublishResult.ForStatus(PublishResultStatus.FinalizeFailed);
        }

        var publishedStatus = PublishStatus.Published;
        var thumbnailStatus = await ApplyThumbnailAsync(
            command,
            platform,
            publishResult.ExternalResourceId,
            thumbnail,
            cancellationToken);
        var isFuture = calendarEvent.ScheduledStartUtc > timeProvider.GetUtcNow();
        return PublishResult.Published(
            new EventPlatformView(
                command.PlatformId,
                platform.Name,
                platform.Type,
                publishedStatus,
                publishResult.ExternalResourceId,
                publishedUtc.Value,
                null,
                PlatformActionPolicy.CanPublish(publishedStatus, isOrphaned: false, isFuture),
                PlatformActionPolicy.CanDeletePublication(
                    publishedStatus,
                    isOrphaned: false,
                    hasExternalResourceId: true,
                    isFuture),
                PlatformActionPolicy.CanPreviewPublishingContent(
                    publishedStatus,
                    isOrphaned: false,
                    hasContentSnapshot: true),
                thumbnailStatus));
    }

    private async Task<PublishThumbnail> LoadThumbnailAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var thumbnail = await thumbnails.GetThumbnailAsync(calendarEventId, cancellationToken);
        if (thumbnail is null)
        {
            return PublishThumbnail.NotConfigured;
        }

        var content = await thumbnailStore.GetAsync(thumbnail.BlobName, cancellationToken);

        return content is null
            ? PublishThumbnail.MissingContent
            : PublishThumbnail.Configured(content);
    }

    private async Task<ThumbnailPublishStatus?> ApplyThumbnailAsync(
        PublishCommand command,
        PlatformView platform,
        string externalResourceId,
        PublishThumbnail thumbnail,
        CancellationToken cancellationToken)
    {
        var thumbnailStatus = EventPlatformProjection.ThumbnailStatusFor(platform.Type);
        if (thumbnailStatus is null)
        {
            return null;
        }

        if (!thumbnail.IsConfigured)
        {
            return ThumbnailPublishStatus.NotConfigured;
        }

        if (thumbnail.Content is null)
        {
            await MarkThumbnailFailedAsync(
                command.CalendarEventId,
                command.PlatformId,
                "stored thumbnail bytes were not found",
                cancellationToken);

            return ThumbnailPublishStatus.Failed;
        }

        var thumbnailPublisher = thumbnailPublishers.Find(platform.Type);
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
                    externalResourceId,
                    platform.PublishSettings,
                    thumbnail.Content),
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
                externalResourceId);

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
                externalResourceId);

            await MarkThumbnailFailedAsync(
                command.CalendarEventId,
                command.PlatformId,
                "provider thumbnail upload failed unexpectedly",
                cancellationToken);

            return ThumbnailPublishStatus.Failed;
        }

        var recorded = await publicationRepository.MarkThumbnailAppliedAsync(
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
        var recorded = await publicationRepository.MarkThumbnailFailedAsync(
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

    private sealed record PublishThumbnail(bool IsConfigured, ThumbnailContent? Content)
    {
        public static PublishThumbnail NotConfigured { get; } = new(false, null);

        public static PublishThumbnail MissingContent { get; } = new(true, null);

        public static PublishThumbnail Configured(ThumbnailContent content) =>
            new(true, content);
    }
}
