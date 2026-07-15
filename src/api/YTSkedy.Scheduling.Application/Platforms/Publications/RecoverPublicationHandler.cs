using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public sealed class RecoverPublicationHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    IPublicationAttemptWriter publicationAttempts,
    PublicationExecutionSettings executionSettings,
    TimeProvider timeProvider,
    ILogger<RecoverPublicationHandler> logger)
{
    public async Task<RecoverPublicationResult> HandleAsync(
        RecoverPublicationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var calendarEvent = await calendarEvents.GetByIdAsync(
            command.CalendarEventId,
            cancellationToken);
        if (calendarEvent is null)
        {
            return Result(RecoverPublicationStatus.EventNotFound);
        }

        var publication = await publications.GetAsync(
            command.CalendarEventId,
            command.PlatformId,
            cancellationToken);
        var platform = await platforms.GetAsync(command.PlatformId, cancellationToken);
        if (publication is null)
        {
            return Result(platform is null
                ? RecoverPublicationStatus.PlatformNotFound
                : RecoverPublicationStatus.PublicationNotFound);
        }

        if (platform is null || publication.IsOrphaned)
        {
            return Result(RecoverPublicationStatus.PlatformDeleted);
        }

        var now = timeProvider.GetUtcNow();
        if (calendarEvent.ScheduledStartUtc <= now)
        {
            return Result(RecoverPublicationStatus.PastStart);
        }

        if (publication.Status != PublishStatus.Publishing)
        {
            return Result(RecoverPublicationStatus.NotPublishing);
        }

        if (!PlatformActionPolicy.CanRecoverPublication(
                publication.Status,
                publication.IsOrphaned,
                isFuture: true,
                publication.UpdatedUtc,
                now,
                executionSettings.StaleAfter))
        {
            return Result(RecoverPublicationStatus.NotStale);
        }

        var writeResult = await publicationAttempts.RecoverStalePublishingAsync(
            command.CalendarEventId,
            command.PlatformId,
            publication.UpdatedUtc,
            cancellationToken);
        if (writeResult != RecoverStalePublishingResult.Recovered)
        {
            return Result(writeResult == RecoverStalePublishingResult.NotFound
                ? RecoverPublicationStatus.PublicationNotFound
                : RecoverPublicationStatus.RowChanged);
        }

        logger.LogInformation(
            "Recovered stale publication for calendar event {CalendarEventId} and platform " +
            "{PlatformId}. Attempt age: {AttemptAge}. External id present: " +
            "{HasExternalResourceId}.",
            command.CalendarEventId,
            command.PlatformId,
            now - publication.UpdatedUtc,
            !string.IsNullOrWhiteSpace(publication.ExternalResourceId));
        return Result(RecoverPublicationStatus.Recovered);
    }

    private static RecoverPublicationResult Result(RecoverPublicationStatus status) => new(status);
}
