using System.Net;
using Google;
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
/// Single infrastructure adapter for the YouTube live broadcasts API,
/// implementing both <see cref="IYouTubePublisher"/> and
/// <see cref="IYouTubeDeleter"/>. It builds one <see cref="YouTubeService"/> from
/// the static refresh-token credentials using the
/// <see cref="YouTubeService.Scope.Youtube"/> scope, which authorizes both
/// <c>liveBroadcasts.insert</c> and <c>liveBroadcasts.delete</c>. Publish maps
/// the request to a broadcast, inserts it, logs success, and rethrows a provider
/// failure so the publish handler releases its reservation and the host returns
/// 500. Delete treats a provider not-found (HTTP 404) as success-equivalent and
/// wraps any other provider failure as a <see cref="YouTubeDeleteException"/> so
/// the host can return 502 and keep the local row. Cancellation propagates
/// unchanged. Google SDK types never cross this boundary, and secrets and tokens
/// are never logged.
/// </summary>
public sealed class YouTubeBroadcastAdapter : IYouTubePublisher, IYouTubeDeleter
{
    private const string ApplicationName = "YTSkedy";

    private readonly YouTubeService _youTubeService;
    private readonly YouTubeBroadcastOptions _broadcastOptions;
    private readonly ILogger<YouTubeBroadcastAdapter> _logger;

    public YouTubeBroadcastAdapter(
        IOptions<YouTubeOptions> youTubeOptions,
        IOptions<YouTubeBroadcastOptions> broadcastOptions,
        ILogger<YouTubeBroadcastAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(youTubeOptions);
        ArgumentNullException.ThrowIfNull(broadcastOptions);
        ArgumentNullException.ThrowIfNull(logger);

        var credentials = youTubeOptions.Value;
        _broadcastOptions = broadcastOptions.Value;
        _logger = logger;

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
        YouTubeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
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

            var created = await _youTubeService.LiveBroadcasts
                .Insert(broadcast, "snippet,status")
                .ExecuteAsync(cancellationToken);

            _logger.LogInformation(
                "Created YouTube broadcast {BroadcastId} scheduled for {ScheduledStartUtc:o}.",
                created.Id,
                request.ScheduledStartUtc);

            return created.Id;
        }
        catch (OperationCanceledException)
        {
            // Respect cancellation: it is not a publish failure, so do not log
            // it as one.
            throw;
        }
        catch (Exception exception)
        {
            // The provider exception carries API-side error details (quota,
            // validation, auth rejection) but not our client secret or tokens,
            // so it is safe to log. The publish handler turns this into a 500
            // and releases the publish reservation.
            _logger.LogError(
                exception,
                "Failed to create YouTube broadcast scheduled for {ScheduledStartUtc:o}.",
                request.ScheduledStartUtc);

            throw;
        }
    }

    public async Task DeleteAsync(
        string youTubeBroadcastId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(youTubeBroadcastId);

        try
        {
            await _youTubeService.LiveBroadcasts
                .Delete(youTubeBroadcastId)
                .ExecuteAsync(cancellationToken);

            _logger.LogInformation(
                "Deleted YouTube live broadcast {BroadcastId}.",
                youTubeBroadcastId);
        }
        catch (GoogleApiException exception)
            when (exception.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // The broadcast is already gone. The intended external end state is
            // already true, so treat this as success-equivalent.
            _logger.LogInformation(
                "YouTube live broadcast {BroadcastId} was already gone; " +
                "treating delete as success-equivalent.",
                youTubeBroadcastId);
        }
        catch (OperationCanceledException)
        {
            // Respect cancellation: do not mask it as a delete failure.
            throw;
        }
        catch (Exception exception)
        {
            // The provider exception carries API-side error details (quota,
            // validation, auth rejection) but not our client secret or tokens,
            // so it is safe to log. The application result carries no message,
            // so provider details never leak past this boundary.
            _logger.LogError(
                exception,
                "Failed to delete YouTube live broadcast {BroadcastId}.",
                youTubeBroadcastId);

            throw new YouTubeDeleteException(
                $"Failed to delete YouTube live broadcast '{youTubeBroadcastId}'.",
                exception);
        }
    }
}
