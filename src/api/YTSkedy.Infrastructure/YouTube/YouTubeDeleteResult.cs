namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Outcome of a YouTube live broadcast delete at the <see cref="IYouTubeClient"/>
/// seam. Provider not-found is represented distinctly from a fresh delete so
/// <see cref="YouTubeDeleter"/> can log the already-gone case differently while
/// still treating it as success-equivalent. Any other provider failure is
/// surfaced as an exception rather than a value here.
/// </summary>
public enum YouTubeDeleteResult
{
    Deleted,
    NotFound
}
