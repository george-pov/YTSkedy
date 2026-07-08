using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.WordPress;

/// <summary>
/// WordPress implementation of <see cref="IPlatformPublisher"/>. It creates a
/// post through the WordPress REST API using Application Password Basic Auth.
/// Secrets and authorization headers are never logged.
/// </summary>
public sealed class WordPressPublisher : IPlatformPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient;
    private readonly WordPressEndpointResolver endpointResolver;
    private readonly ILogger<WordPressPublisher> logger;

    public WordPressPublisher(
        HttpClient httpClient,
        WordPressEndpointResolver endpointResolver,
        ILogger<WordPressPublisher> logger)
    {
        this.httpClient = httpClient;
        this.endpointResolver = endpointResolver;
        this.logger = logger;
    }

    public PlatformType Type => PlatformType.WordPress;

    public async Task<PlatformPublishResult> PublishAsync(
        PlatformPublishRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PublishSettings is not WordPressSettings settings)
        {
            throw new PlatformPublishException(
                "A WordPress publish requires WordPress publish settings.");
        }

        Uri? endpoint = null;

        try
        {
            var root = await endpointResolver.ResolveAsync(settings, cancellationToken);
            endpoint = root.BuildRoute("/wp/v2/posts");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(
                    new WordPressPostRequest(
                        request.Title,
                        request.Description ?? string.Empty,
                        settings.PostStatus),
                    options: JsonOptions)
            };
            httpRequest.Headers.Authorization =
                WordPressRequestSecurity.CreateAuthorizationHeader(settings);

            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogProviderFailure(
                    request,
                    endpoint.Host,
                    response.StatusCode);

                throw new PlatformPublishException(
                    $"WordPress returned HTTP {(int)response.StatusCode} while publishing calendar event '{request.CalendarEventId}'.");
            }

            var body = await response.Content.ReadFromJsonAsync<WordPressPostResponse>(
                JsonOptions,
                cancellationToken);
            if (body?.Id is null or <= 0)
            {
                logger.LogError(
                    "WordPress returned an invalid post id for calendar event {CalendarEventId} " +
                    "and platform {PlatformId} at host {WordPressHost}.",
                    request.CalendarEventId,
                    request.PlatformId,
                    endpoint.Host);

                throw new PlatformPublishException(
                    $"WordPress returned an invalid post id while publishing calendar event '{request.CalendarEventId}'.");
            }

            logger.LogInformation(
                "Created WordPress post {PostId} for calendar event {CalendarEventId} " +
                "and platform {PlatformId} at host {WordPressHost}.",
                body.Id.Value,
                request.CalendarEventId,
                request.PlatformId,
                endpoint.Host);

            return new PlatformPublishResult(
                body.Id.Value.ToString(CultureInfo.InvariantCulture));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            logger.LogError(
                exception,
                "WordPress returned malformed JSON for calendar event {CalendarEventId} " +
                "and platform {PlatformId} at host {WordPressHost}.",
                request.CalendarEventId,
                request.PlatformId,
                WordPressRequestSecurity.GetLogHost(settings, endpoint));

            throw new PlatformPublishException(
                $"WordPress returned malformed JSON while publishing calendar event '{request.CalendarEventId}'.",
                exception);
        }
        catch (PlatformPublishException)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(
                exception,
                "WordPress request failed for calendar event {CalendarEventId} " +
                "and platform {PlatformId} at host {WordPressHost}.",
                request.CalendarEventId,
                request.PlatformId,
                WordPressRequestSecurity.GetLogHost(settings, endpoint));

            throw new PlatformPublishException(
                $"Failed to publish calendar event '{request.CalendarEventId}' to WordPress.",
                exception);
        }
    }

    private void LogProviderFailure(
        PlatformPublishRequest request,
        string host,
        HttpStatusCode statusCode)
    {
        logger.LogError(
            "WordPress returned HTTP {StatusCode} for calendar event {CalendarEventId} " +
            "and platform {PlatformId} at host {WordPressHost}.",
            (int)statusCode,
            request.CalendarEventId,
            request.PlatformId,
            host);
    }

    private sealed record WordPressPostRequest(
        string Title,
        string Content,
        string Status);

    private sealed record WordPressPostResponse(long? Id, string? Link);
}
