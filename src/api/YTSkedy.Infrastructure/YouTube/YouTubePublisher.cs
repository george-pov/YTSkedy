using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Creates a private scheduled YouTube broadcast, then conditionally updates
/// its video resource when category, disclosure, or final visibility settings
/// require <c>videos.update</c>. Existing mutable values in every included part
/// are copied before YTSkedy-owned values are applied.
/// </summary>
public sealed class YouTubePublisher : IPlatformPublisher
{
    private readonly IYouTubePublishClientFactory _clientFactory;
    private readonly ILogger<YouTubePublisher> _logger;

    public YouTubePublisher(
        IYouTubePublishClientFactory clientFactory,
        ILogger<YouTubePublisher> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public PlatformType Type => PlatformType.YouTube;

    public async Task<PlatformPublishResult> PublishAsync(
        PlatformPublishRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PublishSettings is not YouTubeSettings settings)
        {
            throw new PlatformPublishException(
                "A YouTube publish requires YouTube publish settings.");
        }

        string? broadcastId = null;

        try
        {
            var client = _clientFactory.Create(settings.Credentials);
            var broadcast = YouTubeBroadcastFactory.Create(
                request.Title,
                request.Description,
                request.ScheduledStartUtc,
                privacyStatus: "private",
                settings.SelfDeclaredMadeForKids);

            var created = await client.InsertBroadcastAsync(broadcast, cancellationToken);
            broadcastId = created.Id;
            ArgumentException.ThrowIfNullOrWhiteSpace(broadcastId);

            var parts = YouTubeVideoUpdateFactory.RequiredParts(settings);
            if (parts.ApiValue is not null)
            {
                var current = await client.GetVideoAsync(
                    broadcastId,
                    parts.ApiValue,
                    cancellationToken);
                if (current is null)
                {
                    throw new InvalidOperationException(
                        "YouTube did not return the video for the created broadcast.");
                }

                var update = YouTubeVideoUpdateFactory.Create(current, settings, parts);
                await client.UpdateVideoAsync(
                    update.Video,
                    update.Parts,
                    cancellationToken);
            }

            _logger.LogInformation(
                "Created YouTube broadcast {BroadcastId} for calendar event {CalendarEventId} " +
                "scheduled for {ScheduledStartUtc:o}.",
                broadcastId,
                request.CalendarEventId,
                request.ScheduledStartUtc);

            return new PlatformPublishResult(broadcastId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish YouTube broadcast {BroadcastId} for calendar event " +
                "{CalendarEventId} scheduled for {ScheduledStartUtc:o}.",
                broadcastId,
                request.CalendarEventId,
                request.ScheduledStartUtc);

            throw new PlatformPublishException(
                $"Failed to publish calendar event '{request.CalendarEventId}' to YouTube.",
                broadcastId,
                exception);
        }
    }
}
