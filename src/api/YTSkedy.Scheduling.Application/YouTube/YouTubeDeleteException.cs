namespace YTSkedy.Scheduling.Application.YouTube;

/// <summary>
/// Raised by an <see cref="IYouTubeDeleter"/> when deleting a broadcast
/// fails for a reason other than the broadcast already being gone. The message
/// is intentionally generic so provider exception details, tokens, or account
/// data never leak through application results; the infrastructure adapter logs
/// the underlying cause safely.
/// </summary>
public sealed class YouTubeDeleteException : Exception
{
    public YouTubeDeleteException(string message)
        : base(message)
    {
    }

    public YouTubeDeleteException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
