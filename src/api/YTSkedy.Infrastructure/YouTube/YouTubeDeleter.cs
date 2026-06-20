using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Deletes scheduled YouTube live broadcasts through the shared
/// <see cref="IYouTubeClient"/>. Provider not-found is logged and treated as
/// success-equivalent: the adapter completes without throwing so the delete use
/// case sees an already-gone broadcast as a successful delete. Any other provider
/// failure is wrapped as a <see cref="YouTubeDeleteException"/> so the
/// HTTP host can return 502 and keep the local row; provider exception details
/// are logged but never surfaced through the application result. Cancellation
/// propagates unchanged. Secrets and tokens are never logged.
/// </summary>
public sealed class YouTubeDeleter : IYouTubeDeleter
{
    private readonly IYouTubeClient _youTubeClient;
    private readonly ILogger<YouTubeDeleter> _logger;

    public YouTubeDeleter(
        IYouTubeClient youTubeClient,
        ILogger<YouTubeDeleter> logger)
    {
        ArgumentNullException.ThrowIfNull(youTubeClient);
        ArgumentNullException.ThrowIfNull(logger);

        _youTubeClient = youTubeClient;
        _logger = logger;
    }

    public async Task DeleteAsync(
        string youTubeBroadcastId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(youTubeBroadcastId);

        try
        {
            var result = await _youTubeClient.DeleteAsync(
                youTubeBroadcastId,
                cancellationToken);

            if (result == YouTubeDeleteResult.NotFound)
            {
                _logger.LogInformation(
                    "YouTube live broadcast {BroadcastId} was already gone; " +
                    "treating delete as success-equivalent.",
                    youTubeBroadcastId);

                return;
            }

            _logger.LogInformation(
                "Deleted YouTube live broadcast {BroadcastId}.",
                youTubeBroadcastId);
        }
        catch (OperationCanceledException)
        {
            // Respect cancellation: do not mask it as a delete failure.
            throw;
        }
        catch (Exception exception)
        {
            // The provider exception carries API-side error details (quota,
            // validation, auth rejection) but not our client secret or tokens,
            // so it is safe to log. The application result carries no message,
            // so provider details never leak past this boundary.
            _logger.LogError(
                exception,
                "Failed to delete YouTube live broadcast {BroadcastId}.",
                youTubeBroadcastId);

            throw new YouTubeDeleteException(
                $"Failed to delete YouTube live broadcast '{youTubeBroadcastId}'.",
                exception);
        }
    }
}
