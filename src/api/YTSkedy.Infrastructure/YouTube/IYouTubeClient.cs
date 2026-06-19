using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Wraps the generated <c>Google.Apis.YouTube.v3</c> live broadcasts resource
/// (insert and delete) behind one infrastructure abstraction. Both the publish
/// and delete adapters consume it, so the Google SDK is constructed once and each
/// adapter can be unit-tested by faking provider results without mocking
/// generated SDK types or calling live YouTube. The application layer depends
/// only on the product-shaped ports (<see cref="IYouTubePublisher"/> and
/// <see cref="IYouTubeDeleter"/>), never on this seam, and Google SDK
/// types never cross it.
/// </summary>
public interface IYouTubeClient
{
    /// <summary>
    /// Creates a scheduled live broadcast from the request and the shared
    /// broadcast options and returns its broadcast id. Provider failures
    /// propagate as exceptions for the adapter to translate.
    /// </summary>
    Task<string> InsertAsync(
        YouTubeRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the broadcast with the given id. Returns
    /// <see cref="YouTubeDeleteResult.Deleted"/> on provider success
    /// (HTTP 204) and <see cref="YouTubeDeleteResult.NotFound"/> when
    /// the provider reports the broadcast is already gone (HTTP 404). Any other
    /// provider failure propagates as an exception for the adapter to translate.
    /// </summary>
    Task<YouTubeDeleteResult> DeleteAsync(
        string broadcastId,
        CancellationToken cancellationToken);
}

