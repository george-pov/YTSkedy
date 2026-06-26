using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.WordPress;

/// <summary>
/// WordPress implementation of <see cref="IPlatformPublicationDeleter"/>. It
/// hard-deletes the post identified by the stored provider resource id through
/// the WordPress REST API using Application Password Basic Auth. Secrets and
/// authorization headers are never logged.
/// </summary>
public sealed class WordPressPublicationDeleter(
    HttpClient httpClient,
    ILogger<WordPressPublicationDeleter> logger) : IPlatformPublicationDeleter
{
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

        var endpoint = BuildPostDeleteEndpoint(settings, postId);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        httpRequest.Headers.Authorization = CreateAuthorizationHeader(settings);

        try
        {
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
                endpoint.Host);

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

    private static Uri BuildPostDeleteEndpoint(WordPressSettings settings, long postId)
    {
        if (!WordPressSettings.IsValidSiteUrl(settings.SiteUrl) ||
            !Uri.TryCreate(settings.SiteUrl.Trim(), UriKind.Absolute, out var siteUri))
        {
            throw new InvalidOperationException(
                "A WordPress publication delete requires a safe absolute site URL.");
        }

        var baseUrl = siteUri.ToString().TrimEnd('/');
        return new Uri(
            $"{baseUrl}/wp-json/wp/v2/posts/{postId.ToString(CultureInfo.InvariantCulture)}?force=true");
    }

    private static AuthenticationHeaderValue CreateAuthorizationHeader(
        WordPressSettings settings)
    {
        var credentials = $"{settings.Username}:{settings.ApplicationPassword}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        return new AuthenticationHeaderValue("Basic", encoded);
    }

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
