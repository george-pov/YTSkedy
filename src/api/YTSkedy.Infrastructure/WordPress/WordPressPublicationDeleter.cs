using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.WordPress;

/// <summary>
/// WordPress implementation of <see cref="IPlatformPublicationDeleter"/>. It
/// hard-deletes the post identified by the stored provider resource id through
/// the WordPress REST API using Application Password Basic Auth. Secrets and
/// authorization headers are never logged.
/// </summary>
public sealed class WordPressPublicationDeleter : IPlatformPublicationDeleter
{
    private readonly HttpClient httpClient;
    private readonly WordPressEndpointResolver endpointResolver;
    private readonly ILogger<WordPressPublicationDeleter> logger;

    public WordPressPublicationDeleter(
        HttpClient httpClient,
        WordPressEndpointResolver endpointResolver,
        ILogger<WordPressPublicationDeleter> logger)
    {
        this.httpClient = httpClient;
        this.endpointResolver = endpointResolver;
        this.logger = logger;
    }

    public PlatformType Type => PlatformType.WordPress;

    public async Task<PublicationDeleteResult> DeleteAsync(
        PublicationDeleteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PublishSettings is not WordPressSettings settings)
        {
            logger.LogError(
                "Deleting WordPress publication for calendar event {CalendarEventId} and " +
                "platform {PlatformId} requires WordPress publish settings.",
                request.CalendarEventId,
                request.PlatformId);

            return PublicationDeleteResult.Failed;
        }

        if (!TryParsePostId(request.ExternalResourceId, out var postId))
        {
            logger.LogWarning(
                "WordPress external resource id {ExternalResourceId} for calendar event " +
                "{CalendarEventId} and platform {PlatformId} is not a positive post id.",
                request.ExternalResourceId,
                request.CalendarEventId,
                request.PlatformId);

            return PublicationDeleteResult.StateConflict;
        }

        Uri? endpoint = null;

        try
        {
            var root = await endpointResolver.ResolveAsync(settings, cancellationToken);
            endpoint = root.BuildRoute(
                $"/wp/v2/posts/{postId.ToString(CultureInfo.InvariantCulture)}",
                new Dictionary<string, string> { ["force"] = "true" });
            using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, endpoint);
            httpRequest.Headers.Authorization =
                WordPressRequestSecurity.CreateAuthorizationHeader(settings);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Deleted WordPress post {PostId} for calendar event {CalendarEventId} " +
                    "and platform {PlatformId} at host {WordPressHost}.",
                    postId,
                    request.CalendarEventId,
                    request.PlatformId,
                    endpoint.Host);

                return PublicationDeleteResult.Deleted;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation(
                    "WordPress post {PostId} for calendar event {CalendarEventId} and " +
                    "platform {PlatformId} at host {WordPressHost} was already gone.",
                    postId,
                    request.CalendarEventId,
                    request.PlatformId,
                    endpoint.Host);

                return PublicationDeleteResult.AlreadyGone;
            }

            LogProviderFailure(request, endpoint.Host, response.StatusCode);

            return PublicationDeleteResult.Failed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "WordPress delete request failed for calendar event {CalendarEventId} " +
                "and platform {PlatformId} at host {WordPressHost}.",
                request.CalendarEventId,
                request.PlatformId,
                WordPressRequestSecurity.GetLogHost(settings, endpoint));

            return PublicationDeleteResult.Failed;
        }
    }

    private static bool TryParsePostId(string externalResourceId, out long postId) =>
        long.TryParse(
            externalResourceId,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out postId) &&
        postId > 0;

    private void LogProviderFailure(
        PublicationDeleteRequest request,
        string host,
        HttpStatusCode statusCode)
    {
        logger.LogError(
            "WordPress returned HTTP {StatusCode} while deleting calendar event " +
            "{CalendarEventId} publication for platform {PlatformId} at host {WordPressHost}.",
            (int)statusCode,
            request.CalendarEventId,
            request.PlatformId,
            host);
    }
}
