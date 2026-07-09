using Google;
using Google.Apis.Upload;
using Microsoft.Extensions.Logging;
using System.Net;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// YouTube implementation of <see cref="IThumbnailPublisher"/>. It applies a
/// stored calendar-event thumbnail to the already-created YouTube broadcast id
/// that YTSkedy stores as the publication external resource id.
/// </summary>
public sealed class YouTubeThumbnailPublisher : IThumbnailPublisher
{
    private readonly IYouTubeThumbnailClient client;
    private readonly ILogger<YouTubeThumbnailPublisher> logger;

    public YouTubeThumbnailPublisher(ILogger<YouTubeThumbnailPublisher> logger)
        : this(new YouTubeThumbnailClient(), logger)
    {
    }

    internal YouTubeThumbnailPublisher(
        IYouTubeThumbnailClient client,
        ILogger<YouTubeThumbnailPublisher> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    public PlatformType Type => PlatformType.YouTube;

    public async Task PublishAsync(
        ThumbnailPublishRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PublishSettings is not YouTubeSettings settings)
        {
            logger.LogError(
                "Applying a YouTube thumbnail for calendar event {CalendarEventId} and platform " +
                "{PlatformId} requires YouTube publish settings.",
                request.CalendarEventId,
                request.PlatformId);

            throw new ThumbnailPublishException(
                "A YouTube thumbnail publish requires YouTube publish settings.");
        }

        var broadcastId = request.ExternalResourceId;
        try
        {
            await client.SetAsync(
                settings.Credentials,
                broadcastId,
                request.ThumbnailContent,
                cancellationToken);

            logger.LogInformation(
                "Applied thumbnail to YouTube broadcast {BroadcastId} for calendar event " +
                "{CalendarEventId} and platform {PlatformId}.",
                broadcastId,
                request.CalendarEventId,
                request.PlatformId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (YouTubeThumbnailPublishException exception)
        {
            logger.LogWarning(
                "YouTube rejected thumbnail application for broadcast {BroadcastId}, calendar event " +
                "{CalendarEventId}, and platform {PlatformId}. Status: {StatusCode}. Reasons: {Reasons}.",
                broadcastId,
                request.CalendarEventId,
                request.PlatformId,
                exception.StatusCode,
                string.Join(",", exception.Reasons));

            throw new ThumbnailPublishException(
                $"Failed to apply thumbnail for calendar event '{request.CalendarEventId}' to YouTube.",
                exception);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed to apply thumbnail to YouTube broadcast {BroadcastId} for calendar event " +
                "{CalendarEventId} and platform {PlatformId}.",
                broadcastId,
                request.CalendarEventId,
                request.PlatformId);

            throw new ThumbnailPublishException(
                $"Failed to apply thumbnail for calendar event '{request.CalendarEventId}' to YouTube.",
                exception);
        }
    }
}

internal interface IYouTubeThumbnailClient
{
    Task SetAsync(
        YouTubeCredentials credentials,
        string broadcastId,
        ThumbnailContent thumbnailContent,
        CancellationToken cancellationToken);
}

internal sealed class YouTubeThumbnailClient : IYouTubeThumbnailClient
{
    public async Task SetAsync(
        YouTubeCredentials credentials,
        string broadcastId,
        ThumbnailContent thumbnailContent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentException.ThrowIfNullOrWhiteSpace(broadcastId);
        ArgumentNullException.ThrowIfNull(thumbnailContent);

        try
        {
            var service = YouTubeServiceFactory.Create(credentials);
            using var content = new MemoryStream(thumbnailContent.Content, writable: false);

            var upload = service.Thumbnails.Set(
                broadcastId,
                content,
                thumbnailContent.ContentType);
            var progress = await upload.UploadAsync(cancellationToken);

            if (progress.Status != UploadStatus.Completed)
            {
                throw YouTubeThumbnailPublishException.From(progress);
            }
        }
        catch (GoogleApiException exception)
        {
            throw YouTubeThumbnailPublishException.From(exception);
        }
    }
}

internal sealed class YouTubeThumbnailPublishException : Exception
{
    public YouTubeThumbnailPublishException(
        HttpStatusCode? statusCode,
        IReadOnlyList<string> reasons,
        Exception? innerException = null)
        : base("YouTube thumbnail publish failed.", innerException)
    {
        StatusCode = statusCode;
        Reasons = reasons;
    }

    public HttpStatusCode? StatusCode { get; }

    public IReadOnlyList<string> Reasons { get; }

    internal static YouTubeThumbnailPublishException From(GoogleApiException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return new YouTubeThumbnailPublishException(
            exception.HttpStatusCode,
            ReasonsFrom(exception),
            exception);
    }

    internal static YouTubeThumbnailPublishException From(IUploadProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        if (progress.Exception is GoogleApiException googleException)
        {
            return From(googleException);
        }

        return new YouTubeThumbnailPublishException(
            null,
            [],
            progress.Exception);
    }

    private static IReadOnlyList<string> ReasonsFrom(GoogleApiException exception) =>
        exception.Error?.Errors?
            .Select(error => error.Reason)
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Cast<string>()
            .ToArray() ?? [];
}
