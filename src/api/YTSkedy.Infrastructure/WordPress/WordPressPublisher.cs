using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
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
    private readonly TimeProvider timeProvider;
    private readonly ILogger<WordPressPublisher> logger;

    public WordPressPublisher(
        HttpClient httpClient,
        WordPressEndpointResolver endpointResolver,
        TimeProvider timeProvider,
        ILogger<WordPressPublisher> logger)
    {
        this.httpClient = httpClient;
        this.endpointResolver = endpointResolver;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public PlatformType Type => PlatformType.WordPress;

    public async Task<PlatformPublishResult> PublishAsync(
        PlatformPublishRequest request,
        IPlatformPublishCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(checkpoint);

        if (request.PublishSettings is not WordPressSettings settings)
        {
            throw new PlatformPublishException(
                "A WordPress publish requires WordPress publish settings.");
        }

        Uri? endpoint = null;
        WordPressRoot? resolvedRoot = null;
        var stage = "discovery";
        var postRequest = CreatePostRequest(request, settings);

        try
        {
            resolvedRoot = await endpointResolver.ResolveAsync(
                settings,
                cancellationToken,
                request.AttemptId);
            endpoint = resolvedRoot.BuildRoute("/wp/v2/posts");
            stage = "create_post";
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(
                    postRequest,
                    options: JsonOptions)
            };
            WordPressRequestHeaders.AddClientIdentification(httpRequest, request.AttemptId);
            httpRequest.Headers.Authorization =
                WordPressRequestSecurity.CreateAuthorizationHeader(settings);

            var requestStarted = timeProvider.GetTimestamp();
            using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var duration = timeProvider.GetElapsedTime(requestStarted);
            if (!response.IsSuccessStatusCode)
            {
                var failure = await WordPressPublishFailureReader.ReadAsync(
                    response,
                    timeProvider,
                    cancellationToken);
                LogProviderFailure(
                    request,
                    endpoint.Host,
                    resolvedRoot,
                    failure,
                    duration,
                    response.Content.Headers.ContentType?.MediaType,
                    response.Content.Headers.ContentLength);
                SetFailureActivityTags(failure, resolvedRoot, duration);

                throw new PlatformPublishException(failure);
            }

            var body = await response.Content.ReadFromJsonAsync<WordPressPostResponse>(
                JsonOptions,
                cancellationToken);
            if (body?.Id is null or <= 0)
            {
                logger.LogError(
                    "WordPress returned an invalid post id for calendar event {CalendarEventId} " +
                    "and platform {PlatformId} at host {WordPressHost}. Publish attempt: " +
                    "{PublishAttemptId}. Request stage: {RequestStage}.",
                    request.CalendarEventId,
                    request.PlatformId,
                    endpoint.Host,
                    request.AttemptId,
                    stage);

                throw new PlatformPublishException(
                    new PlatformPublishFailure(
                        PlatformPublishFailureCodes.WordPressInvalidResponse,
                        "WordPress returned an invalid post identifier.",
                        stage));
            }

            var externalResourceId = body.Id.Value.ToString(CultureInfo.InvariantCulture);
            try
            {
                await checkpoint.SaveExternalResourceIdAsync(
                    externalResourceId,
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new PlatformPublishException(
                    $"WordPress post creation succeeded for calendar event " +
                    $"'{request.CalendarEventId}', but its id could not be checkpointed.",
                    externalResourceId,
                    exception);
            }

            logger.LogInformation(
                "Created WordPress post {PostId} for calendar event {CalendarEventId} " +
                "and platform {PlatformId} at host {WordPressHost}. Publish attempt: " +
                "{PublishAttemptId}. Request stage: {RequestStage}. Duration: {DurationMs} ms. " +
                "Discovery cache hit: {DiscoveryCacheHit}. Provider request count: " +
                "{ProviderRequestCount}. Endpoint style: {EndpointStyle}.",
                body.Id.Value,
                request.CalendarEventId,
                request.PlatformId,
                endpoint.Host,
                request.AttemptId,
                stage,
                duration.TotalMilliseconds,
                resolvedRoot.DiscoveryCacheHit,
                resolvedRoot.DiscoveryRequestCount + 1,
                resolvedRoot.EndpointStyle);

            return new PlatformPublishResult(externalResourceId);
        }
        catch (OperationCanceledException exception) when (
            !PublishCancellationClassifier.IsCallerCancellation(exception, cancellationToken))
        {
            throw PublishCancellationClassifier.ToPublishException(
                exception,
                "WordPress",
                request.CalendarEventId);
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
                "and platform {PlatformId} at host {WordPressHost}. Publish attempt: " +
                "{PublishAttemptId}. Request stage: {RequestStage}.",
                request.CalendarEventId,
                request.PlatformId,
                WordPressRequestSecurity.GetLogHost(settings, endpoint),
                request.AttemptId,
                stage);

            throw new PlatformPublishException(
                new PlatformPublishFailure(
                    PlatformPublishFailureCodes.WordPressInvalidResponse,
                    "WordPress returned an invalid JSON response.",
                    stage),
                innerException: exception);
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
                "and platform {PlatformId} at host {WordPressHost}. Publish attempt: " +
                "{PublishAttemptId}. Request stage: {RequestStage}.",
                request.CalendarEventId,
                request.PlatformId,
                WordPressRequestSecurity.GetLogHost(settings, endpoint),
                request.AttemptId,
                stage);

            throw new PlatformPublishException(
                new PlatformPublishFailure(
                    stage == "discovery"
                        ? PlatformPublishFailureCodes.WordPressDiscoveryFailed
                        : PlatformPublishFailureCodes.WordPressProviderError,
                    stage == "discovery"
                        ? "YTSkedy could not discover the WordPress REST API."
                        : "YTSkedy could not reach WordPress while creating the post.",
                    stage,
                    VerificationRequired: stage != "discovery"),
                innerException: exception);
        }
    }

    private void LogProviderFailure(
        PlatformPublishRequest request,
        string host,
        WordPressRoot root,
        PlatformPublishFailure failure,
        TimeSpan duration,
        string? contentType,
        long? contentLength)
    {
        logger.LogError(
            "WordPress publish failed for calendar event {CalendarEventId} and platform " +
            "{PlatformId} at host {WordPressHost}. Publish attempt: {PublishAttemptId}. " +
            "Failure code: {FailureCode}. Request stage: {RequestStage}. HTTP status: " +
            "{StatusCode}. Provider error code: {ProviderErrorCode}. Retry after: " +
            "{RetryAfterUtc}. Duration: {DurationMs} ms. " +
            "Discovery cache hit: {DiscoveryCacheHit}. Provider request count: " +
            "{ProviderRequestCount}. Endpoint style: {EndpointStyle}. Response content type: " +
            "{ResponseContentType}. Response content length: {ResponseContentLength}.",
            request.CalendarEventId,
            request.PlatformId,
            host,
            request.AttemptId,
            failure.Code,
            failure.Stage,
            failure.ProviderStatus,
            failure.ProviderErrorCode,
            failure.RetryAfterUtc,
            duration.TotalMilliseconds,
            root.DiscoveryCacheHit,
            root.DiscoveryRequestCount + 1,
            root.EndpointStyle,
            contentType,
            contentLength);
    }

    private static void SetFailureActivityTags(
        PlatformPublishFailure failure,
        WordPressRoot root,
        TimeSpan duration)
    {
        Activity.Current?.SetTag("ytskedy.wordpress.stage", failure.Stage);
        Activity.Current?.SetTag("ytskedy.wordpress.status_code", failure.ProviderStatus);
        Activity.Current?.SetTag("ytskedy.wordpress.error_code", failure.ProviderErrorCode);
        Activity.Current?.SetTag("ytskedy.wordpress.retry_after_utc", failure.RetryAfterUtc);
        Activity.Current?.SetTag("ytskedy.wordpress.duration_ms", duration.TotalMilliseconds);
        Activity.Current?.SetTag("ytskedy.wordpress.discovery_cache_hit", root.DiscoveryCacheHit);
        Activity.Current?.SetTag(
            "ytskedy.wordpress.provider_request_count",
            root.DiscoveryRequestCount + 1);
        Activity.Current?.SetTag("ytskedy.wordpress.endpoint_style", root.EndpointStyle);
    }

    private WordPressPostRequest CreatePostRequest(
        PlatformPublishRequest request,
        WordPressSettings settings)
    {
        string? dateGmt = null;
        if (settings.PostStatus == WordPressSettings.ScheduledPostStatus)
        {
            if (!settings.TryGetScheduledPostUtc(
                    request.ScheduledStartUtc,
                    out var scheduledPostUtc))
            {
                logger.LogWarning(
                    "WordPress scheduled post offset for calendar event {CalendarEventId} " +
                    "and platform {PlatformId} is invalid.",
                    request.CalendarEventId,
                    request.PlatformId);

                throw new PlatformPublishValidationException(
                    "WordPress scheduled post offset is invalid.");
            }

            if (scheduledPostUtc <= timeProvider.GetUtcNow())
            {
                logger.LogWarning(
                    "WordPress scheduled post time {ScheduledPostUtc} for calendar event " +
                    "{CalendarEventId} and platform {PlatformId} is not in the future.",
                    scheduledPostUtc,
                    request.CalendarEventId,
                    request.PlatformId);

                throw new PlatformPublishValidationException(
                    "WordPress scheduled post time must be in the future.");
            }

            dateGmt = FormatDateGmt(scheduledPostUtc);
        }

        return new WordPressPostRequest(
            request.Title,
            request.Description ?? string.Empty,
            settings.PostStatus,
            settings.Sticky,
            dateGmt,
            settings.CategoryIds.Count == 0
                ? null
                : settings.CategoryIds);
    }

    private static string FormatDateGmt(DateTimeOffset scheduledPostUtc) =>
        scheduledPostUtc
            .UtcDateTime
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private sealed record WordPressPostRequest(
        string Title,
        string Content,
        string Status,
        bool Sticky,
        [property: JsonPropertyName("date_gmt")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        string? DateGmt,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<long>? Categories);

    private sealed record WordPressPostResponse(long? Id, string? Link);

}
