using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Content;
using YTSkedy.Scheduling.Application.Platforms.EventPlatforms;
using YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

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
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    IPublicationAttemptWriter publicationAttempts,
    IPlatformTypeAdapterSelector<IPlatformPublisher> publishers,
    PublicationThumbnailApplier thumbnailApplier,
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

        var startResult = await publicationAttempts.StartPublishingAsync(
            attempt,
            cancellationToken);
        if (startResult == StartPublicationResult.Conflict)
        {
            // A concurrent publish won the conditional start write.
            return PublishResult.ForStatus(PublishResultStatus.PublishInProgress);
        }

        PublicationThumbnail thumbnail;
        try
        {
            thumbnail = await thumbnailApplier.LoadAsync(
                command.CalendarEventId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Failed to load thumbnail content for calendar event {CalendarEventId} before " +
                "publishing platform {PlatformId}.",
                command.CalendarEventId,
                command.PlatformId);

            await publicationAttempts.ReleasePublishingAsync(
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

            await publicationAttempts.ReleasePublishingAsync(
                command.CalendarEventId,
                command.PlatformId,
                cancellationToken);

            return PublishResult.ForStatus(PublishResultStatus.ProviderFailed);
        }

        var publishedUtc = await publicationAttempts.MarkPublishedAsync(
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

        var thumbnailStatus = await thumbnailApplier.ApplyAsync(
            new PublicationThumbnailCommand(
                command.CalendarEventId,
                command.PlatformId,
                platform,
                publishResult.ExternalResourceId,
                thumbnail),
            cancellationToken);

        return PublishResult.Published(
            EventPlatformProjection.ProjectPublished(
                calendarEvent,
                platform,
                publishResult.ExternalResourceId,
                publishedUtc.Value,
                timeProvider.GetUtcNow(),
                thumbnailStatus));
    }
}
