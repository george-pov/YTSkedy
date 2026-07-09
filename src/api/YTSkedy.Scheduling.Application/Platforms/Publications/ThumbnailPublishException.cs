namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public sealed class ThumbnailPublishException : Exception
{
    public ThumbnailPublishException(string message)
        : base(message)
    {
    }

    public ThumbnailPublishException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
