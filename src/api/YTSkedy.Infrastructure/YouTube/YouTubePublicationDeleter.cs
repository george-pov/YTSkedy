using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.Extensions.Logging;
using System.Net;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// YouTube implementation of <see cref="IPlatformPublicationDeleter"/>. It
/// deletes the scheduled live broadcast identified by the stored provider
/// resource id. Google SDK types and OAuth credential handling stay inside
/// infrastructure, and secrets or tokens are never logged.
/// </summary>
public sealed class YouTubePublicationDeleter : IPlatformPublicationDeleter
{
    private readonly IYouTubeLiveBroadcastDeletionClient client;
    private readonly ILogger<YouTubePublicationDeleter> logger;

    public YouTubePublicationDeleter(ILogger<YouTubePublicationDeleter> logger)
        : this(new YouTubeLiveBroadcastDeletionClient(), logger)
    {
    }

    internal YouTubePublicationDeleter(
        IYouTubeLiveBroadcastDeletionClient client,
        ILogger<YouTubePublicationDeleter> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public PlatformType Type => PlatformType.YouTube;

    public async Task<PublicationDeleteResult> DeleteAsync(
        PublicationDeleteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PublishSettings is not YouTubeSettings settings)
        {
            logger.LogError(
                "Deleting YouTube publication for calendar event {CalendarEventId} and " +
                "platform {PlatformId} requires YouTube publish settings.",
                request.CalendarEventId,
                request.PlatformId);

            return PublicationDeleteResult.Failed;
        }

        try
        {
            await client.DeleteAsync(
                settings.Credentials,
                request.ExternalResourceId,
                cancellationToken);

            logger.LogInformation(
                "Deleted YouTube broadcast {BroadcastId} for calendar event {CalendarEventId} " +
                "and platform {PlatformId}.",
                request.ExternalResourceId,
                request.CalendarEventId,
                request.PlatformId);

            return PublicationDeleteResult.Deleted;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (YouTubePublicationDeleteException exception)
            when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation(
                "YouTube broadcast {BroadcastId} for calendar event {CalendarEventId} and " +
                "platform {PlatformId} was already gone.",
                request.ExternalResourceId,
                request.CalendarEventId,
                request.PlatformId);

            return PublicationDeleteResult.AlreadyGone;
        }
        catch (YouTubePublicationDeleteException exception)
            when (IsStateConflict(exception))
        {
            logger.LogWarning(
                "YouTube broadcast {BroadcastId} for calendar event {CalendarEventId} and " +
                "platform {PlatformId} cannot be deleted in its current provider state.",
                request.ExternalResourceId,
                request.CalendarEventId,
                request.PlatformId);

            return PublicationDeleteResult.StateConflict;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to delete YouTube broadcast {BroadcastId} for calendar event " +
                "{CalendarEventId} and platform {PlatformId}.",
                request.ExternalResourceId,
                request.CalendarEventId,
                request.PlatformId);

            return PublicationDeleteResult.Failed;
        }
    }

    private static bool IsStateConflict(YouTubePublicationDeleteException exception) =>
        exception.StatusCode == HttpStatusCode.Conflict ||
        exception.Reasons.Any(
            reason =>
                string.Equals(
                    reason,
                    "liveBroadcastDeletionNotAllowed",
                    StringComparison.OrdinalIgnoreCase) ||
                reason.Contains("notAllowed", StringComparison.OrdinalIgnoreCase));
}

internal interface IYouTubeLiveBroadcastDeletionClient
{
    Task DeleteAsync(
        YouTubeCredentials credentials,
        string broadcastId,
        CancellationToken cancellationToken);
}

internal sealed class YouTubeLiveBroadcastDeletionClient : IYouTubeLiveBroadcastDeletionClient
{
    private const string ApplicationName = "YTSkedy";

    public async Task DeleteAsync(
        YouTubeCredentials credentials,
        string broadcastId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(broadcastId);

        try
        {
            var service = CreateService(credentials);

            await service.LiveBroadcasts
                .Delete(broadcastId)
                .ExecuteAsync(cancellationToken);
        }
        catch (GoogleApiException exception)
        {
            throw YouTubePublicationDeleteException.From(exception);
        }
    }

    private static YouTubeService CreateService(YouTubeCredentials credentials)
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

internal sealed class YouTubePublicationDeleteException : Exception
{
    public YouTubePublicationDeleteException(
        HttpStatusCode statusCode,
        IReadOnlyList<string> reasons,
        Exception? innerException = null)
        : base("YouTube publication delete failed.", innerException)
    {
        StatusCode = statusCode;
        Reasons = reasons;
    }

    public HttpStatusCode StatusCode { get; }

    public IReadOnlyList<string> Reasons { get; }

    internal static YouTubePublicationDeleteException From(GoogleApiException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var reasons = exception.Error?.Errors?
            .Select(error => error.Reason)
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Cast<string>()
            .ToArray() ?? [];

        return new YouTubePublicationDeleteException(
            exception.HttpStatusCode,
            reasons,
            exception);
    }
}
