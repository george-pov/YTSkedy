using Microsoft.Extensions.Logging;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubeThumbnailPublisherTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;
    private const string BroadcastId = SchedulingSampleIds.YouTubeBroadcastId;
    private const string ClientSecret = "client-secret-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public void Type_IsYouTube()
    {
        var publisher = CreatePublisher(new FakeThumbnailClient());

        Assert.Equal(PlatformType.YouTube, publisher.Type);
    }

    [Fact]
    public async Task PublishAsync_Success_UploadsThumbnailForBroadcast()
    {
        var client = new FakeThumbnailClient();
        var publisher = CreatePublisher(client);
        var request = Request();

        await publisher.PublishAsync(request, CancellationToken.None);

        Assert.Equal(BroadcastId, client.BroadcastId);
        Assert.Equal("client-id", client.Credentials!.ClientId);
        Assert.Same(request.ThumbnailContent, client.ThumbnailContent);
    }

    [Fact]
    public async Task PublishAsync_NonYouTubeSettings_Throws()
    {
        var publisher = CreatePublisher(new FakeThumbnailClient());

        await Assert.ThrowsAsync<ThumbnailPublishException>(
            () => publisher.PublishAsync(Request(new OtherSettings()), CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_ProviderFailure_ThrowsWithoutLoggingSecrets()
    {
        var logger = new CapturingLogger<YouTubeThumbnailPublisher>();
        var publisher = CreatePublisher(
            new FakeThumbnailClient
            {
                Throws = new YouTubeThumbnailPublishException(null, ["invalidImage"])
            },
            logger);

        await Assert.ThrowsAsync<ThumbnailPublishException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));

        Assert.DoesNotContain(ClientSecret, logger.Text);
        Assert.DoesNotContain(RefreshToken, logger.Text);
        Assert.Contains("invalidImage", logger.Text);
    }

    [Fact]
    public async Task PublishAsync_OperationCanceledException_Propagates()
    {
        var publisher = CreatePublisher(
            new FakeThumbnailClient
            {
                Throws = new OperationCanceledException()
            });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => publisher.PublishAsync(Request(), CancellationToken.None));
    }

    private static YouTubeThumbnailPublisher CreatePublisher(
        FakeThumbnailClient client,
        ILogger<YouTubeThumbnailPublisher>? logger = null) =>
        new(client, logger ?? new CapturingLogger<YouTubeThumbnailPublisher>());

    private static ThumbnailPublishRequest Request(PublishSettings? settings = null) =>
        new(
            CalendarEventId,
            PlatformId,
            BroadcastId,
            settings ?? new YouTubeSettings(
                new YouTubeCredentials("client-id", ClientSecret, RefreshToken),
                "private",
                false),
            new ThumbnailContent([1, 2, 3], "image/png"));

    private sealed record OtherSettings : PublishSettings;

    private sealed class FakeThumbnailClient : IYouTubeThumbnailClient
    {
        public YouTubeCredentials? Credentials { get; private set; }

        public string? BroadcastId { get; private set; }

        public ThumbnailContent? ThumbnailContent { get; private set; }

        public Exception? Throws { get; init; }

        public Task SetAsync(
            YouTubeCredentials credentials,
            string broadcastId,
            ThumbnailContent thumbnailContent,
            CancellationToken cancellationToken)
        {
            Credentials = credentials;
            BroadcastId = broadcastId;
            ThumbnailContent = thumbnailContent;

            if (Throws is not null)
            {
                throw Throws;
            }

            return Task.CompletedTask;
        }
    }

}
