using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.EventPlatforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Deletes one completed platform publication. Provider cleanup runs before the
/// local row is removed, and local removal is conditional on the row still being
/// the same published resource.
/// </summary>
public sealed class DeletePublicationHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    IPublicationCleanupWriter publicationCleanup,
    IPublicationIndexWriter publicationIndex,
    IPlatformTypeAdapterSelector<IPlatformPublicationDeleter> deleters,
    TimeProvider timeProvider,
    ILogger<DeletePublicationHandler> logger)
{
    public async Task<DeletePublicationResult> HandleAsync(
        DeletePublicationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await calendarEvents.GetByIdAsync(
            command.CalendarEventId,
            cancellationToken);
        if (calendarEvent is null)
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.EventNotFound);
        }

        var publication = await publications.GetAsync(
            command.CalendarEventId,
            command.PlatformId,
            cancellationToken);
        var platform = await platforms.GetAsync(command.PlatformId, cancellationToken);

        if (platform is null)
        {
            return publication is not null && publication.IsOrphaned
                ? DeletePublicationResult.ForStatus(DeletePublicationStatus.Orphaned)
                : DeletePublicationResult.ForStatus(DeletePublicationStatus.PlatformNotFound);
        }

        if (publication is null || publication.Status == PublishStatus.NotPublished)
        {
            return DeletePublicationResult.Success(
                DeletePublicationStatus.AlreadyNotPublished,
                EventPlatformMapper.MapNotPublished(
                    calendarEvent,
                    platform,
                    timeProvider.GetUtcNow()));
        }

        if (publication.IsOrphaned)
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.Orphaned);
        }

        if (publication.Status == PublishStatus.Publishing)
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.PublishInProgress);
        }

        if (publication.Status != PublishStatus.Published)
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.RowChanged);
        }

        if (calendarEvent.ScheduledStartUtc <= timeProvider.GetUtcNow())
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.PastStart);
        }

        if (string.IsNullOrWhiteSpace(publication.ExternalResourceId))
        {
            return DeletePublicationResult.ForStatus(
                DeletePublicationStatus.MissingExternalResourceId);
        }

        if (!PublicationTargetPolicy.Matches(platform, publication.TargetSnapshot))
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.TargetMismatch);
        }

        var deleter = deleters.Find(platform.Type);
        if (deleter is null)
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.ProviderNotSupported);
        }

        PublicationDeleteResult providerResult;
        try
        {
            providerResult = await deleter.DeleteAsync(
                new PublicationDeleteRequest(
                    command.CalendarEventId,
                    command.PlatformId,
                    platform.PublishSettings,
                    publication.ExternalResourceId),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Deleting provider resource {ExternalResourceId} for calendar event " +
                "{CalendarEventId} and platform {PlatformId} failed.",
                publication.ExternalResourceId,
                command.CalendarEventId,
                command.PlatformId);

            return DeletePublicationResult.ForStatus(DeletePublicationStatus.ProviderFailed);
        }

        if (providerResult.Status == PublicationDeleteStatus.StateConflict)
        {
            return DeletePublicationResult.ForStatus(
                DeletePublicationStatus.ProviderStateConflict);
        }

        if (providerResult.Status == PublicationDeleteStatus.Failed)
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.ProviderFailed);
        }

        var deleteResult = await publicationCleanup.DeletePublishedAsync(
            command.CalendarEventId,
            command.PlatformId,
            publication.ExternalResourceId,
            cancellationToken);

        if (deleteResult is not (
                DeletePublishedResult.Deleted or
                DeletePublishedResult.NotFound))
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.RowChanged);
        }

        try
        {
            if (!await publicationIndex.RemovePublishedPlatformAsync(
                    command.CalendarEventId,
                    command.PlatformId,
                    cancellationToken))
            {
                logger.LogError(
                    "Publication index operation {Operation} failed for calendar event " +
                    "{CalendarEventId} and platform {PlatformId}.",
                    "RemovePublishedPlatform",
                    command.CalendarEventId,
                    command.PlatformId);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "Publication index operation {Operation} failed for calendar event " +
                "{CalendarEventId} and platform {PlatformId}.",
                "RemovePublishedPlatform",
                command.CalendarEventId,
                command.PlatformId);
        }

        return DeletePublicationResult.Success(
            DeletePublicationStatus.Deleted,
            EventPlatformMapper.MapNotPublished(
                calendarEvent,
                platform,
                timeProvider.GetUtcNow()));
    }
}
