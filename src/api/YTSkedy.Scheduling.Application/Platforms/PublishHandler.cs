using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Publishes one calendar event to one selected platform. The flow loads the
/// event, the platform, and the selected provider, then guards state and content:
/// an existing row that is orphaned, published, or publishing is a conflict; the
/// start must be in the future; and the first slice requires an English title.
/// It then reserves the publication row (a conditional write, so a concurrent
/// publish yields a conflict), calls the provider, and finalizes the row with the
/// external resource id. A provider failure releases the reservation and surfaces
/// an upstream failure; a finalize failure after the external resource was
/// created is recorded for follow-up because the first-slice provider abstraction
/// has no cleanup.
/// </summary>
public sealed class PublishHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    IPlatformPublicationRepository publicationRepository,
    IPlatformPublisherSelector publishers,
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

        // Selecting the provider before reserving avoids leaving a Publishing row
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
            // Any persisted row is terminal or in-flight for this slice: a
            // NotPublished pair has no row. Orphaned history cannot be republished.
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

        var englishContent = calendarEvent.Descriptions.FirstOrDefault(
            description => description.IsEnglish && !string.IsNullOrWhiteSpace(description.Title));
        if (englishContent is null)
        {
            return PublishResult.ForStatus(PublishResultStatus.MissingEnglishTitle);
        }

        var reservation = new PlatformPublicationReservation(
            command.CalendarEventId,
            command.PlatformId,
            platform.Name,
            platform.Type,
            platform.PublishSettings);

        var reservationResult = await publicationRepository.ReserveAsync(
            reservation,
            cancellationToken);
        if (reservationResult == ReservePublicationResult.Conflict)
        {
            // A concurrent publish won the conditional reservation write.
            return PublishResult.ForStatus(PublishResultStatus.PublishInProgress);
        }

        PlatformPublishResult publishResult;
        try
        {
            publishResult = await publisher.PublishAsync(
                new PlatformPublishRequest(
                    command.CalendarEventId,
                    command.PlatformId,
                    platform.PublishSettings,
                    englishContent.Title,
                    englishContent.Description,
                    calendarEvent.ScheduledStartUtc),
                cancellationToken);
        }
        catch (PlatformPublishException exception)
        {
            // Release the reservation so the pair returns to NotPublished and can
            // be retried. The provider already logged secret-safe details.
            logger.LogError(
                exception,
                "Publishing calendar event {CalendarEventId} to platform {PlatformId} failed.",
                command.CalendarEventId,
                command.PlatformId);

            await publicationRepository.ReleaseAsync(
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
            // The external resource exists but the reserved row vanished before it
            // could be finalized. There is no provider cleanup in this slice, so
            // record the orphaned external resource for operator follow-up.
            logger.LogError(
                "Published calendar event {CalendarEventId} to platform {PlatformId} as external " +
                "resource {ExternalResourceId}, but the publication row could not be finalized.",
                command.CalendarEventId,
                command.PlatformId,
                publishResult.ExternalResourceId);

            return PublishResult.ForStatus(PublishResultStatus.FinalizeFailed);
        }

        return PublishResult.Published(
            platform.Name,
            platform.Type,
            publishResult.ExternalResourceId,
            publishedUtc.Value);
    }
}
