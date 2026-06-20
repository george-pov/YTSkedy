namespace YTSkedy.Scheduling.Application.YouTube;

/// <summary>
/// Application-layer port for deleting a scheduled YouTube live broadcast.
/// Implemented in the infrastructure layer so the domain and application
/// projects never reference the Google client libraries directly.
/// </summary>
public interface IYouTubeDeleter
{
    /// <summary>
    /// Deletes the YouTube live broadcast with the given id. Completing without
    /// throwing means the intended end state holds: the broadcast was removed,
    /// or the provider reported it was already gone, which is treated as
    /// success-equivalent. Throws <see cref="YouTubeDeleteException"/> for any
    /// other provider failure so the caller can keep local state and report an
    /// upstream error.
    /// </summary>
    Task DeleteAsync(
        string youTubeBroadcastId,
        CancellationToken cancellationToken);
}
