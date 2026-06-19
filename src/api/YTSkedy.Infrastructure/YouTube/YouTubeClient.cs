using System.Net;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Options;
using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Real <see cref="IYouTubeClient"/> backed by the official
/// <c>Google.Apis.YouTube.v3</c> client. It builds one <see cref="YouTubeService"/>
/// from the static refresh-token credentials and uses the
/// <see cref="YouTubeService.Scope.Youtube"/> scope, which the YouTube Data API
/// authorizes for both <c>liveBroadcasts.insert</c> and
/// <c>liveBroadcasts.delete</c>. Insert returns the created broadcast id; delete
/// returns HTTP 204 on success and throws <see cref="GoogleApiException"/> with
/// status 404 when the broadcast is already gone, which is translated here. Every
/// other failure propagates to the adapter, and Google SDK types never cross this
/// boundary.
/// </summary>
public sealed class YouTubeClient : IYouTubeClient
{
    private const string ApplicationName = "YTSkedy";

    private readonly YouTubeService _youTubeService;
    private readonly YouTubeBroadcastOptions _broadcastOptions;

    public YouTubeClient(
        IOptions<YouTubeOptions> youTubeOptions,
        IOptions<YouTubeBroadcastOptions> broadcastOptions)
    {
        ArgumentNullException.ThrowIfNull(youTubeOptions);
        ArgumentNullException.ThrowIfNull(broadcastOptions);

        var credentials = youTubeOptions.Value;
        _broadcastOptions = broadcastOptions.Value;

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = credentials.ClientId,
                ClientSecret = credentials.ClientSecret
            },
            Scopes = [YouTubeService.Scope.Youtube]
        });

        var token = new TokenResponse { RefreshToken = credentials.RefreshToken };
        var credential = new UserCredential(flow, ApplicationName, token);

        _youTubeService = new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });
    }

    public async Task<string> InsertAsync(
        YouTubeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var broadcast = new LiveBroadcast
        {
            Snippet = new LiveBroadcastSnippet
            {
                Title = request.Title,
                Description = request.Description ?? string.Empty,
                ScheduledStartTimeDateTimeOffset = request.ScheduledStartUtc
            },
            Status = new LiveBroadcastStatus
            {
                PrivacyStatus = _broadcastOptions.PrivacyStatus,
                SelfDeclaredMadeForKids = _broadcastOptions.SelfDeclaredMadeForKids
            }
        };

        var insertRequest = _youTubeService.LiveBroadcasts.Insert(broadcast, "snippet,status");
        var created = await insertRequest.ExecuteAsync(cancellationToken);

        return created.Id;
    }

    public async Task<YouTubeDeleteResult> DeleteAsync(
        string broadcastId,
        CancellationToken cancellationToken)
    {
        var deleteRequest = _youTubeService.LiveBroadcasts.Delete(broadcastId);

        try
        {
            await deleteRequest.ExecuteAsync(cancellationToken);

            return YouTubeDeleteResult.Deleted;
        }
        catch (GoogleApiException exception)
            when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // The broadcast is already gone. The intended external end state is
            // already true, so the adapter treats this as success-equivalent.
            return YouTubeDeleteResult.NotFound;
        }
    }
}
