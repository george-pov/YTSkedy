namespace YTSkedy.Scheduling.Application.YouTube;

/// <summary>
/// Application-layer port for deleting a scheduled YouTube live broadcast.
/// Implemented in the infrastructure layer so the domain and application
/// projects never reference the Google client libraries directly.
/// </summary>
public interface IYouTubeDeleter
{
    /// <summary>
    /// Deletes the YouTube live broadcast with the given id. Returns
    /// <see cref="YouTubeDeleteResult.Deleted"/> when the broadcast was
    /// removed and <see cref="YouTubeDeleteResult.NotFound"/> when the
    /// provider reports it is already gone. Throws
    /// <see cref="YouTubeDeleteException"/> for any other provider
    /// failure so the caller can keep local state and report an upstream error.
    /// </summary>
    Task<YouTubeDeleteResult> DeleteAsync(
        string youTubeBroadcastId,
        CancellationToken cancellationToken);
}
