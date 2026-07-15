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
/// external resource id. Caught non-cancellation failures after the attempt
/// starts become retryable failed rows, retaining the provider id when known.
/// </summary>
public sealed class PublishHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    IPublicationAttemptWriter publicationAttempts,
    PublicationIndexUpdater publicationIndexUpdater,
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
        var existingPublicationStatus = ValidateExistingPublication(existing);
        if (existingPublicationStatus is not null)
        {
            return PublishResult.ForStatus(existingPublicationStatus.Value);
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

            return await RecordFailedAsync(
                command,
                externalResourceId: null,
                cancellationToken);
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
        catch (PlatformPublishValidationException exception)
        {
            logger.LogWarning(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} " +
                "failed provider-specific validation.",
                command.CalendarEventId,
                command.PlatformId);

            return await RecordFailedAsync(
                command,
                externalResourceId: null,
                cancellationToken);
        }
        catch (PlatformPublishException exception)
        {
            logger.LogError(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} failed.",
                command.CalendarEventId,
                command.PlatformId);

            return await RecordFailedAsync(
                command,
                exception.ExternalResourceId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} failed " +
                "with unexpected provider exception type {ExceptionType}.",
                command.CalendarEventId,
                command.PlatformId,
                exception.GetType().FullName);

            return await RecordFailedAsync(
                command,
                externalResourceId: null,
                cancellationToken);
        }

        DateTimeOffset? publishedUtc;
        try
        {
            publishedUtc = await publicationAttempts.MarkPublishedAsync(
                command.CalendarEventId,
                command.PlatformId,
                publishResult.ExternalResourceId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Published calendar event {CalendarEventId} to platform {PlatformId} as " +
                "external resource {ExternalResourceId}, but finalizing publication state failed.",
                command.CalendarEventId,
                command.PlatformId,
                publishResult.ExternalResourceId);

            return await RecordFailedAsync(
                command,
                publishResult.ExternalResourceId,
                cancellationToken);
        }
        if (publishedUtc is null)
        {
            logger.LogError(
                "Published calendar event {CalendarEventId} to platform {PlatformId} as external " +
                "resource {ExternalResourceId}, but the publication row could not be finalized.",
                command.CalendarEventId,
                command.PlatformId,
                publishResult.ExternalResourceId);

            return await RecordFailedAsync(
                command,
                publishResult.ExternalResourceId,
                cancellationToken);
        }

        await publicationIndexUpdater.AddPublishedPlatformAsync(
            command.CalendarEventId,
            command.PlatformId,
            cancellationToken);

        var thumbnailStatus = await thumbnailApplier.ApplyAsync(
            new PublicationThumbnailCommand(
                command.CalendarEventId,
                command.PlatformId,
                platform,
                publishResult.ExternalResourceId,
                thumbnail),
            cancellationToken);

        return PublishResult.Published(
            EventPlatformMapper.MapPublished(
                calendarEvent,
                platform,
                publishResult.ExternalResourceId,
                publishedUtc.Value,
                timeProvider.GetUtcNow(),
                thumbnailStatus));
    }

    private static PublishResultStatus? ValidateExistingPublication(
        PlatformPublication? existing)
    {
        if (existing is null)
        {
            return null;
        }

        // A NotPublished pair has no row. Orphaned history cannot be republished.
        if (existing.IsOrphaned)
        {
            return PublishResultStatus.PlatformDeleted;
        }

        return existing.Status switch
        {
            PublishStatus.Published => PublishResultStatus.AlreadyPublished,
            PublishStatus.Publishing => PublishResultStatus.PublishInProgress,
            _ => null
        };
    }

    private async Task<PublishResult> RecordFailedAsync(
        PublishCommand command,
        string? externalResourceId,
        CancellationToken cancellationToken)
    {
        MarkFailedResult result;
        try
        {
            result = await publicationAttempts.MarkFailedAsync(
                command.CalendarEventId,
                command.PlatformId,
                externalResourceId,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogCritical(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} could not " +
                "record Failed. External resource id: {ExternalResourceId}.",
                command.CalendarEventId,
                command.PlatformId,
                externalResourceId);

            return PublishResult.ForStatus(PublishResultStatus.FinalizeFailed);
        }
        if (result == MarkFailedResult.Marked)
        {
            return PublishResult.ForStatus(PublishResultStatus.Failed);
        }

        logger.LogCritical(
            "Publishing calendar event {CalendarEventId} to platform {PlatformId} could not " +
            "record a final publication state. Failed-state result: {MarkFailedResult}. " +
            "External resource id: {ExternalResourceId}.",
            command.CalendarEventId,
            command.PlatformId,
            result,
            externalResourceId);

        return PublishResult.ForStatus(PublishResultStatus.FinalizeFailed);
    }
}
