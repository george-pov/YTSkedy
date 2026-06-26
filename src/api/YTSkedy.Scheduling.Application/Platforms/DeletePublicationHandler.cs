using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Deletes one completed platform publication. Provider cleanup runs before the
/// local row is removed, and local removal is conditional on the row still being
/// the same published resource.
/// </summary>
public sealed class DeletePublicationHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    IPlatformPublicationRepository publicationRepository,
    IPublicationDeleterSelector deleters,
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
                ProjectNotPublished(calendarEvent, platform));
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

        var deleteResult = await publicationRepository.DeletePublishedAsync(
            command.CalendarEventId,
            command.PlatformId,
            publication.ExternalResourceId,
            cancellationToken);

        return deleteResult switch
        {
            DeletePublishedResult.Deleted or DeletePublishedResult.NotFound =>
                DeletePublicationResult.Success(
                    DeletePublicationStatus.Deleted,
                    ProjectNotPublished(calendarEvent, platform)),
            _ => DeletePublicationResult.ForStatus(DeletePublicationStatus.RowChanged)
        };
    }

    private EventPlatformView ProjectNotPublished(
        CalendarEventView calendarEvent,
        PlatformView platform) =>
        EventPlatformProjection.Project(
            calendarEvent,
            [platform],
            [],
            timeProvider.GetUtcNow()).Single();
}
