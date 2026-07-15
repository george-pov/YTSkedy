using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubePublisherTests
{
    private const string BroadcastId = SchedulingSampleIds.YouTubeBroadcastId;
    private const string ClientSecret = "client-secret-value";
    private const string RefreshToken = "refresh-token-value";

    [Fact]
    public void Type_IsYouTube()
    {
        var publisher = CreatePublisher(new FakePublishClient());

        Assert.Equal(PlatformType.YouTube, publisher.Type);
    }

    [Fact]
    public async Task PublishAsync_DefaultSettings_CreatesPrivateBroadcastWithoutVideoUpdate()
    {
        var client = new FakePublishClient();
        var factory = new FakePublishClientFactory(client);
        var publisher = CreatePublisher(factory);
        var checkpoint = new RecordingPublishCheckpoint();

        var result = await publisher.PublishAsync(
            Request(Settings()),
            checkpoint,
            CancellationToken.None);

        Assert.Equal(BroadcastId, result.ExternalResourceId);
        Assert.Equal("client-id", factory.Credentials!.ClientId);
        Assert.Equal("private", client.InsertedBroadcast!.Status.PrivacyStatus);
        Assert.False(client.InsertedBroadcast.Status.SelfDeclaredMadeForKids);
        Assert.Equal("English title", client.InsertedBroadcast.Snippet.Title);
        Assert.Equal("English description", client.InsertedBroadcast.Snippet.Description);
        Assert.Equal(0, client.GetVideoCalls);
        Assert.Equal(0, client.UpdateVideoCalls);
        Assert.Equal([BroadcastId], checkpoint.ExternalResourceIds);
    }

    [Fact]
    public async Task PublishAsync_CategoryDisclosureAndVisibility_ReadsAndUpdatesRequiredParts()
    {
        var client = new FakePublishClient
        {
            CurrentVideo = Video()
        };
        var publisher = CreatePublisher(client);

        await publisher.PublishAsync(
            Request(Settings("public", "27", containsSyntheticMedia: true)),
            new RecordingPublishCheckpoint(),
            CancellationToken.None);

        Assert.Equal("snippet,status", client.RequestedParts);
        Assert.Equal("snippet,status", client.UpdatedParts);
        Assert.Equal("27", client.UpdatedVideo!.Snippet.CategoryId);
        Assert.Equal("Original title", client.UpdatedVideo.Snippet.Title);
        Assert.Equal("Original description", client.UpdatedVideo.Snippet.Description);
        Assert.Equal(["one", "two"], client.UpdatedVideo.Snippet.Tags);
        Assert.Equal("en", client.UpdatedVideo.Snippet.DefaultLanguage);
        Assert.Equal("public", client.UpdatedVideo.Status.PrivacyStatus);
        Assert.True(client.UpdatedVideo.Status.ContainsSyntheticMedia);
        Assert.False(client.UpdatedVideo.Status.SelfDeclaredMadeForKids);
        Assert.True(client.UpdatedVideo.Status.Embeddable);
        Assert.Equal("youtube", client.UpdatedVideo.Status.License);
        Assert.True(client.UpdatedVideo.Status.PublicStatsViewable);
        Assert.Null(client.UpdatedVideo.Status.PublishAtDateTimeOffset);
    }

    [Fact]
    public async Task PublishAsync_UpdateFailure_RetainsCreatedBroadcastIdAndDoesNotDelete()
    {
        var client = new FakePublishClient
        {
            CurrentVideo = Video(),
            UpdateThrows = new InvalidOperationException("provider rejected update")
        };
        var publisher = CreatePublisher(client);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(
                Request(Settings(categoryId: "999999")),
                new RecordingPublishCheckpoint(),
                CancellationToken.None));

        Assert.Equal(BroadcastId, exception.ExternalResourceId);
        Assert.Equal(1, client.UpdateVideoCalls);
    }

    [Fact]
    public async Task PublishAsync_CheckpointFailure_StopsLaterMetadataAndCarriesCreatedId()
    {
        var client = new FakePublishClient { CurrentVideo = Video() };
        var publisher = CreatePublisher(client);
        var checkpoint = new RecordingPublishCheckpoint
        {
            Throws = new InvalidOperationException("storage unavailable")
        };

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(() =>
            publisher.PublishAsync(
                Request(Settings(categoryId: "27")),
                checkpoint,
                CancellationToken.None));

        Assert.Equal(BroadcastId, exception.ExternalResourceId);
        Assert.Equal([BroadcastId], checkpoint.ExternalResourceIds);
        Assert.Equal(0, client.GetVideoCalls);
        Assert.Equal(0, client.UpdateVideoCalls);
    }

    [Fact]
    public async Task PublishAsync_CallerCancellation_PropagatesOriginalCancellation()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var client = new FakePublishClient
        {
            InsertThrows = new OperationCanceledException(caller.Token)
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreatePublisher(client).PublishAsync(
                Request(Settings()),
                new RecordingPublishCheckpoint(),
                caller.Token));
    }

    [Fact]
    public async Task PublishAsync_DependencyTimeout_IsClassified()
    {
        var client = new FakePublishClient
        {
            InsertThrows = new TaskCanceledException(
                "provider timeout",
                new TimeoutException("deadline"))
        };

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(() =>
            CreatePublisher(client).PublishAsync(
                Request(Settings()),
                new RecordingPublishCheckpoint(),
                CancellationToken.None));

        Assert.Equal(PlatformPublishFailureKind.Timeout, exception.FailureKind);
    }

    [Fact]
    public async Task PublishAsync_ProviderFailure_ThrowsWithoutLoggingSecrets()
    {
        var logger = new CapturingLogger<YouTubePublisher>();
        var client = new FakePublishClient
        {
            InsertThrows = new InvalidOperationException("provider failed")
        };
        var publisher = CreatePublisher(client, logger);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(
                Request(Settings()),
                new RecordingPublishCheckpoint(),
                CancellationToken.None));

        Assert.Null(exception.ExternalResourceId);
        Assert.DoesNotContain(ClientSecret, logger.Text);
        Assert.DoesNotContain(RefreshToken, logger.Text);
    }

    [Fact]
    public async Task PublishAsync_UnexpectedOperationCanceledException_IsClassified()
    {
        var client = new FakePublishClient
        {
            InsertThrows = new OperationCanceledException()
        };
        var publisher = CreatePublisher(client);

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(
                Request(Settings()),
                new RecordingPublishCheckpoint(),
                CancellationToken.None));

        Assert.Equal(PlatformPublishFailureKind.UnexpectedCancellation, exception.FailureKind);
    }

    [Fact]
    public async Task PublishAsync_NonYouTubeSettings_Throws()
    {
        var publisher = CreatePublisher(new FakePublishClient());

        await Assert.ThrowsAsync<PlatformPublishException>(
            () => publisher.PublishAsync(
                Request(new OtherSettings()),
                new RecordingPublishCheckpoint(),
                CancellationToken.None));
    }

    private static YouTubePublisher CreatePublisher(
        FakePublishClient client,
        ILogger<YouTubePublisher>? logger = null) =>
        CreatePublisher(new FakePublishClientFactory(client), logger);

    private static YouTubePublisher CreatePublisher(
        IYouTubePublishClientFactory factory,
        ILogger<YouTubePublisher>? logger = null) =>
        new(factory, logger ?? new CapturingLogger<YouTubePublisher>());

    private static PlatformPublishRequest Request(PublishSettings settings) =>
        new(
            SchedulingSampleIds.CalendarEventId,
            SchedulingSampleIds.YouTubePlatformId,
            settings,
            "English title",
            "English description",
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero));

    private static YouTubeSettings Settings(
        string privacyStatus = "private",
        string? categoryId = null,
        bool containsSyntheticMedia = false) =>
        new(
            new YouTubeCredentials("client-id", ClientSecret, RefreshToken),
            privacyStatus,
            false,
            categoryId,
            containsSyntheticMedia);

    private static Video Video() =>
        new()
        {
            Id = BroadcastId,
            Snippet = new VideoSnippet
            {
                CategoryId = "22",
                DefaultLanguage = "en",
                Description = "Original description",
                Tags = ["one", "two"],
                Title = "Original title"
            },
            Status = new VideoStatus
            {
                Embeddable = true,
                License = "youtube",
                PrivacyStatus = "private",
                PublicStatsViewable = true,
                PublishAtDateTimeOffset =
                    new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero),
                SelfDeclaredMadeForKids = false
            }
        };

    private sealed record OtherSettings : PublishSettings;

    private sealed class FakePublishClientFactory(
        IYouTubePublishClient client) : IYouTubePublishClientFactory
    {
        public YouTubeCredentials? Credentials { get; private set; }

        public IYouTubePublishClient Create(YouTubeCredentials credentials)
        {
            Credentials = credentials;
            return client;
        }
    }

    private sealed class FakePublishClient : IYouTubePublishClient
    {
        public LiveBroadcast? InsertedBroadcast { get; private set; }

        public Video? CurrentVideo { get; init; }

        public Video? UpdatedVideo { get; private set; }

        public string? RequestedParts { get; private set; }

        public string? UpdatedParts { get; private set; }

        public int GetVideoCalls { get; private set; }

        public int UpdateVideoCalls { get; private set; }

        public Exception? InsertThrows { get; init; }

        public Exception? UpdateThrows { get; init; }

        public Task<LiveBroadcast> InsertBroadcastAsync(
            LiveBroadcast broadcast,
            CancellationToken cancellationToken)
        {
            InsertedBroadcast = broadcast;
            if (InsertThrows is not null)
            {
                throw InsertThrows;
            }

            return Task.FromResult(new LiveBroadcast { Id = BroadcastId });
        }

        public Task<Video?> GetVideoAsync(
            string videoId,
            string parts,
            CancellationToken cancellationToken)
        {
            GetVideoCalls++;
            RequestedParts = parts;
            return Task.FromResult(CurrentVideo);
        }

        public Task<Video> UpdateVideoAsync(
            Video video,
            string parts,
            CancellationToken cancellationToken)
        {
            UpdateVideoCalls++;
            UpdatedVideo = video;
            UpdatedParts = parts;
            if (UpdateThrows is not null)
            {
                throw UpdateThrows;
            }

            return Task.FromResult(video);
        }
    }
}
