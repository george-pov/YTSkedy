using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.EventPlatforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.CalendarEvents;
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
    PublicationIndexUpdater publicationIndexUpdater,
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

        var eligibilityStatus = ValidateDeletionEligibility(
            calendarEvent,
            platform,
            publication,
            timeProvider.GetUtcNow());
        if (eligibilityStatus is not null)
        {
            return DeletePublicationResult.ForStatus(eligibilityStatus.Value);
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
                    publication.ExternalResourceId!),
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
            publication.ExternalResourceId!,
            cancellationToken);

        if (deleteResult is not (
                DeletePublishedResult.Deleted or
                DeletePublishedResult.NotFound))
        {
            return DeletePublicationResult.ForStatus(DeletePublicationStatus.RowChanged);
        }

        await publicationIndexUpdater.RemovePublishedPlatformAsync(
            command.CalendarEventId,
            command.PlatformId,
            cancellationToken);

        return DeletePublicationResult.Success(
            DeletePublicationStatus.Deleted,
            EventPlatformMapper.MapNotPublished(
                calendarEvent,
                platform,
                timeProvider.GetUtcNow()));
    }

    private static DeletePublicationStatus? ValidateDeletionEligibility(
        CalendarEventView calendarEvent,
        PlatformView platform,
        PlatformPublication publication,
        DateTimeOffset nowUtc)
    {
        if (publication.IsOrphaned)
        {
            return DeletePublicationStatus.Orphaned;
        }

        if (publication.Status == PublishStatus.Publishing)
        {
            return DeletePublicationStatus.PublishInProgress;
        }

        if (publication.Status != PublishStatus.Published)
        {
            return DeletePublicationStatus.RowChanged;
        }

        if (calendarEvent.ScheduledStartUtc <= nowUtc)
        {
            return DeletePublicationStatus.PastStart;
        }

        if (string.IsNullOrWhiteSpace(publication.ExternalResourceId))
        {
            return DeletePublicationStatus.MissingExternalResourceId;
        }

        return PublicationTargetPolicy.Matches(platform, publication.TargetSnapshot)
            ? null
            : DeletePublicationStatus.TargetMismatch;
    }
}
