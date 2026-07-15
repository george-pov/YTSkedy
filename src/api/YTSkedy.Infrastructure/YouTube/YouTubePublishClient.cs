using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Creates a credential-bound YouTube publishing client for one publish
/// operation.
/// </summary>
public interface IYouTubePublishClientFactory
{
    IYouTubePublishClient Create(YouTubeCredentials credentials);
}

public sealed class YouTubePublishClientFactory : IYouTubePublishClientFactory
{
    public IYouTubePublishClient Create(YouTubeCredentials credentials) =>
        new YouTubePublishClient(YouTubeServiceFactory.Create(credentials));
}

/// <summary>
/// Narrow YouTube API surface used to create and configure one scheduled
/// broadcast.
/// </summary>
public interface IYouTubePublishClient
{
    Task<LiveBroadcast> InsertBroadcastAsync(
        LiveBroadcast broadcast,
        CancellationToken cancellationToken);

    Task<Video?> GetVideoAsync(
        string videoId,
        string parts,
        CancellationToken cancellationToken);

    Task<Video> UpdateVideoAsync(
        Video video,
        string parts,
        CancellationToken cancellationToken);
}

public sealed class YouTubePublishClient(YouTubeService service) : IYouTubePublishClient
{
    public Task<LiveBroadcast> InsertBroadcastAsync(
        LiveBroadcast broadcast,
        CancellationToken cancellationToken) =>
        service.LiveBroadcasts
            .Insert(broadcast, "snippet,status")
            .ExecuteAsync(cancellationToken);

    public async Task<Video?> GetVideoAsync(
        string videoId,
        string parts,
        CancellationToken cancellationToken)
    {
        var list = service.Videos.List(parts);
        list.Id = videoId;
        var response = await list.ExecuteAsync(cancellationToken);
        return response.Items.SingleOrDefault();
    }

    public Task<Video> UpdateVideoAsync(
        Video video,
        string parts,
        CancellationToken cancellationToken) =>
        service.Videos
            .Update(video, parts)
            .ExecuteAsync(cancellationToken);
}
