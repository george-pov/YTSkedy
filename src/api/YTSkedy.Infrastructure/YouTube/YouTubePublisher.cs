using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Creates scheduled YouTube live broadcasts through the shared
/// <see cref="IYouTubeClient"/>. The client owns the Google SDK call and
/// the broadcast options; this adapter maps the publish use case and logs. A
/// provider failure is logged and rethrown so the publish handler releases its
/// reservation and the host returns 500. Cancellation propagates unchanged
/// without being logged as a failure. Secrets and tokens are never logged.
/// </summary>
public sealed class YouTubePublisher : IYouTubePublisher
{
    private readonly IYouTubeClient _youTubeClient;
    private readonly ILogger<YouTubePublisher> _logger;

    public YouTubePublisher(
        IYouTubeClient youTubeClient,
        ILogger<YouTubePublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(youTubeClient);
        ArgumentNullException.ThrowIfNull(logger);

        _youTubeClient = youTubeClient;
        _logger = logger;
    }

    public async Task<string> PublishAsync(
        YouTubeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var broadcastId = await _youTubeClient.InsertAsync(request, cancellationToken);

            _logger.LogInformation(
                "Created YouTube broadcast {BroadcastId} scheduled for {ScheduledStartUtc:o}.",
                broadcastId,
                request.ScheduledStartUtc);

            return broadcastId;
        }
        catch (OperationCanceledException)
        {
            // Respect cancellation: it is not a publish failure, so do not log
            // it as one. Mirrors YouTubeDeleter.
            throw;
        }
        catch (Exception exception)
        {
            // The provider exception carries API-side error details (quota,
            // validation, auth rejection) but not our client secret or tokens,
            // so it is safe to log. The publish handler turns this into a 500
            // and releases the publish reservation.
            _logger.LogError(
                exception,
                "Failed to create YouTube broadcast scheduled for {ScheduledStartUtc:o}.",
                request.ScheduledStartUtc);

            throw;
        }
    }
}
