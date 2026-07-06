using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// YouTube implementation of <see cref="IPlatformPublisher"/>. It builds a
/// <see cref="YouTubeService"/> from the selected platform's stored credential
/// values and creates a scheduled live broadcast with the privacy and
/// made-for-kids settings from the platform's <see cref="YouTubeSettings"/>.
/// The created broadcast id is returned as the provider-neutral external
/// resource id. Google SDK types never cross this boundary, and secrets and
/// tokens are never logged.
/// </summary>
public sealed class YouTubePublisher(
    ILogger<YouTubePublisher> logger) : IPlatformPublisher
{
    public PlatformType Type => PlatformType.YouTube;

    public async Task<PlatformPublishResult> PublishAsync(
        PlatformPublishRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Selection is by platform type, so the settings are YouTube settings;
        // guard defensively rather than trusting the cast.
        if (request.PublishSettings is not YouTubeSettings settings)
        {
            throw new PlatformPublishException(
                "A YouTube publish requires YouTube publish settings.");
        }

        var broadcast = YouTubeBroadcastFactory.Create(
            request.Title,
            request.Description,
            request.ScheduledStartUtc,
            settings.PrivacyStatus,
            settings.SelfDeclaredMadeForKids);

        try
        {
            var service = YouTubeServiceFactory.Create(settings.Credentials);

            var created = await service.LiveBroadcasts
                .Insert(broadcast, "snippet,status")
                .ExecuteAsync(cancellationToken);

            logger.LogInformation(
                "Created YouTube broadcast {BroadcastId} for calendar event {CalendarEventId} " +
                "scheduled for {ScheduledStartUtc:o}.",
                created.Id,
                request.CalendarEventId,
                request.ScheduledStartUtc);

            return new PlatformPublishResult(created.Id);
        }
        catch (OperationCanceledException)
        {
            // Respect cancellation: it is not a publish failure.
            throw;
        }
        catch (Exception exception)
        {
            // Provider error details (quota, validation, auth rejection) are safe
            // to log; the client secret and tokens are not part of the exception.
            logger.LogError(
                exception,
                "Failed to create YouTube broadcast for calendar event {CalendarEventId} " +
                "scheduled for {ScheduledStartUtc:o}.",
                request.CalendarEventId,
                request.ScheduledStartUtc);

            throw new PlatformPublishException(
                $"Failed to publish calendar event '{request.CalendarEventId}' to YouTube.",
                exception);
        }
    }
}
