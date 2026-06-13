namespace YTSkedy.Scheduling.Application.YouTube;

/// <summary>
/// Application-layer port for creating a scheduled YouTube live broadcast.
/// Implemented in the infrastructure layer so the domain and application
/// projects never reference the Google client libraries directly.
/// </summary>
public interface IYouTubeBroadcastPublisher
{
    /// <summary>
    /// Creates a scheduled YouTube live broadcast and returns its broadcast id.
    /// </summary>
    Task<string> PublishAsync(
        YouTubeBroadcastRequest request,
        CancellationToken cancellationToken);
}
