using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YTSkedy.Scheduling.Application.YouTube;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Creates scheduled YouTube live broadcasts through the official
/// <c>Google.Apis.YouTube.v3</c> client. A <see cref="UserCredential"/> built
/// from the static refresh token lets the client obtain and refresh access
/// tokens silently. Secrets and tokens are never logged.
/// </summary>
public sealed class YouTubeBroadcastPublisher : IYouTubeBroadcastPublisher
{
    private const string ApplicationName = "YTSkedy";

    private readonly YouTubeService _youTubeService;
    private readonly YouTubeBroadcastOptions _broadcastOptions;
    private readonly ILogger<YouTubeBroadcastPublisher> _logger;

    public YouTubeBroadcastPublisher(
        IOptions<YouTubeOptions> youTubeOptions,
        IOptions<YouTubeBroadcastOptions> broadcastOptions,
        ILogger<YouTubeBroadcastPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(youTubeOptions);
        ArgumentNullException.ThrowIfNull(broadcastOptions);
        ArgumentNullException.ThrowIfNull(logger);

        var credentials = youTubeOptions.Value;
        this._broadcastOptions = broadcastOptions.Value;
        this._logger = logger;

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

    public async Task<string> PublishAsync(
        YouTubeBroadcastRequest request,
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

        try
        {
            var created = await insertRequest.ExecuteAsync(cancellationToken);

            _logger.LogInformation(
                "Created YouTube live broadcast {BroadcastId} scheduled for " +
                "{ScheduledStartUtc:o} with privacy {PrivacyStatus}.",
                created.Id,
                request.ScheduledStartUtc,
                _broadcastOptions.PrivacyStatus);

            return created.Id;
        }
        catch (Exception exception)
        {
            // The Google API exception carries API-side error details (quota,
            // validation, auth rejection) but not our client secret or tokens,
            // so it is safe to log. The handler turns this into a 500 and
            // releases the publish reservation.
            _logger.LogError(
                exception,
                "Failed to create YouTube live broadcast scheduled for " +
                "{ScheduledStartUtc:o} with privacy {PrivacyStatus}.",
                request.ScheduledStartUtc,
                _broadcastOptions.PrivacyStatus);

            throw;
        }
    }
}
