using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// YouTube implementation of <see cref="IPlatformPublisher"/>. It resolves the
/// selected platform's non-secret credentials reference to channel secrets,
/// builds a <see cref="YouTubeService"/> for that channel, and creates a
/// scheduled live broadcast with the privacy and made-for-kids settings from the
/// platform's <see cref="YouTubeSettings"/>. The created broadcast id is returned
/// as the provider-neutral external resource id. Unconfigured credentials and
/// provider failures throw <see cref="PlatformPublishException"/>; Google SDK
/// types never cross this boundary, and secrets and tokens are never logged.
/// </summary>
public sealed class YouTubePublisher(
    IYouTubeChannelCredentialStore credentialStore,
    ILogger<YouTubePublisher> logger) : IPlatformPublisher
{
    private const string ApplicationName = "YTSkedy";

    public PlatformType Type => PlatformType.YouTube;

    public async Task<PlatformPublishResult> PublishAsync(
        PlatformPublishRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Selection is by platform type, so the settings are YouTube settings;
        // guard defensively rather than trusting the cast.
        if (request.PublishSettings is not YouTubeSettings settings)
        {
            throw new PlatformPublishException(
                "A YouTube publish requires YouTube publish settings.");
        }

        var credentials = credentialStore.Find(settings.Credentials);
        if (credentials is null)
        {
            // The reference is a non-secret name, safe to log to aid setup. No
            // secret material is available on this path to leak.
            logger.LogWarning(
                "YouTube credentials reference {CredentialsReference} is not configured.",
                settings.Credentials);

            throw new PlatformPublishException(
                $"YouTube credentials '{settings.Credentials}' are not configured.");
        }

        var broadcast = YouTubeBroadcastFactory.Create(
            request.Title,
            request.Description,
            request.ScheduledStartUtc,
            settings.PrivacyStatus,
            settings.SelfDeclaredMadeForKids);

        try
        {
            var service = CreateService(credentials);

            var created = await service.LiveBroadcasts
                .Insert(broadcast, "snippet,status")
                .ExecuteAsync(cancellationToken);

            logger.LogInformation(
                "Created YouTube broadcast {BroadcastId} for calendar event {CalendarEventId} " +
                "scheduled for {ScheduledStartUtc:o}.",
                created.Id,
                request.CalendarEventId,
                request.ScheduledStartUtc);

            return new PlatformPublishResult(created.Id);
        }
        catch (OperationCanceledException)
        {
            // Respect cancellation: it is not a publish failure.
            throw;
        }
        catch (Exception exception)
        {
            // Provider error details (quota, validation, auth rejection) are safe
            // to log; the client secret and tokens are not part of the exception.
            logger.LogError(
                exception,
                "Failed to create YouTube broadcast for calendar event {CalendarEventId} " +
                "scheduled for {ScheduledStartUtc:o}.",
                request.CalendarEventId,
                request.ScheduledStartUtc);

            throw new PlatformPublishException(
                $"Failed to publish calendar event '{request.CalendarEventId}' to YouTube.",
                exception);
        }
    }

    private static YouTubeService CreateService(YouTubeChannelCredentials credentials)
    {
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

        return new YouTubeService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });
    }
}
