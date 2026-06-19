namespace YTSkedy.Scheduling.Application.YouTube;

/// <summary>
/// Result of deleting a YouTube live broadcast through
/// <see cref="IYouTubeDeleter"/>. Provider not-found is represented
/// distinctly from a fresh delete so the delete use case can treat both as
/// success-equivalent by policy: the intended external end state (no broadcast)
/// is already true. Failures other than not-found are surfaced as a
/// <see cref="YouTubeDeleteException"/>, not as a value here.
/// </summary>
public enum YouTubeDeleteResult
{
    Deleted,
    NotFound
}
