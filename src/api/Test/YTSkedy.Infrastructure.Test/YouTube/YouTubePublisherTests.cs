using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using YTSkedy.Infrastructure.Test.TestSupport;
using YTSkedy.Infrastructure.YouTube;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.Infrastructure.Test.YouTube;

public class YouTubePublisherTests
{
    private const string BroadcastId = SchedulingSampleIds.YouTubeBroadcastId;
    private const string ClientSecret = "client-secret-value";
    private const string RefreshToken = "refresh-token-value";
    private readonly Mock<IYouTubePublishClient> _client = new();
    private readonly Mock<IYouTubePublishClientFactory> _factory = new();
    private readonly Mock<ILogger<YouTubePublisher>> _logger = new();
    private readonly YouTubePublisher _publisher;

    public YouTubePublisherTests()
    {
        _factory
            .Setup(candidate => candidate.Create(It.IsAny<YouTubeCredentials>()))
            .Returns(_client.Object);
        _publisher = new YouTubePublisher(_factory.Object, _logger.Object);
    }

    [Fact]
    public void Type_IsYouTube()
    {
        Assert.Equal(PlatformType.YouTube, _publisher.Type);
    }

    [Fact]
    public async Task PublishAsync_DefaultSettings_CreatesPrivateBroadcastWithoutVideoUpdate()
    {
        LiveBroadcast? insertedBroadcast = null;
        _client
            .Setup(candidate => candidate.InsertBroadcastAsync(
                It.IsAny<LiveBroadcast>(),
                CancellationToken.None))
            .Callback<LiveBroadcast, CancellationToken>(
                (broadcast, _) => insertedBroadcast = broadcast)
            .ReturnsAsync(new LiveBroadcast { Id = BroadcastId });
        var checkpoint = new Mock<IPlatformPublishCheckpoint>();
        checkpoint
            .Setup(candidate => candidate.SaveExternalResourceIdAsync(
                BroadcastId,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        var result = await _publisher.PublishAsync(
            Request(Settings()),
            checkpoint.Object,
            CancellationToken.None);

        Assert.Equal(BroadcastId, result.ExternalResourceId);
        _factory.Verify(candidate => candidate.Create(It.Is<YouTubeCredentials>(credentials =>
            credentials.ClientId == "client-id")));
        Assert.NotNull(insertedBroadcast);
        Assert.Equal("private", insertedBroadcast!.Status.PrivacyStatus);
        Assert.False(insertedBroadcast.Status.SelfDeclaredMadeForKids);
        Assert.Equal("English title", insertedBroadcast.Snippet.Title);
        Assert.Equal("English description", insertedBroadcast.Snippet.Description);
        _client.Verify(candidate => candidate.GetVideoAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _client.Verify(candidate => candidate.UpdateVideoAsync(
            It.IsAny<Video>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        checkpoint.Verify(candidate => candidate.SaveExternalResourceIdAsync(
            BroadcastId,
            CancellationToken.None), Times.Once());
    }

    [Fact]
    public async Task PublishAsync_CategoryDisclosureAndVisibility_ReadsAndUpdatesRequiredParts()
    {
        _client
            .Setup(candidate => candidate.InsertBroadcastAsync(
                It.IsAny<LiveBroadcast>(),
                CancellationToken.None))
            .ReturnsAsync(new LiveBroadcast { Id = BroadcastId });
        _client
            .Setup(candidate => candidate.GetVideoAsync(
                BroadcastId,
                "snippet,status",
                CancellationToken.None))
            .ReturnsAsync(Video());
        Video? updatedVideo = null;
        _client
            .Setup(candidate => candidate.UpdateVideoAsync(
                It.IsAny<Video>(),
                "snippet,status",
                CancellationToken.None))
            .Callback<Video, string, CancellationToken>(
                (video, _, _) => updatedVideo = video)
            .Returns<Video, string, CancellationToken>(
                (video, _, _) => Task.FromResult(video));
        await _publisher.PublishAsync(
            Request(Settings("public", "27", containsSyntheticMedia: true)),
            CancellationToken.None);

        Assert.NotNull(updatedVideo);
        Assert.Equal("27", updatedVideo!.Snippet.CategoryId);
        Assert.Equal("Original title", updatedVideo.Snippet.Title);
        Assert.Equal("Original description", updatedVideo.Snippet.Description);
        Assert.Equal(["one", "two"], updatedVideo.Snippet.Tags);
        Assert.Equal("en", updatedVideo.Snippet.DefaultLanguage);
        Assert.Equal("public", updatedVideo.Status.PrivacyStatus);
        Assert.True(updatedVideo.Status.ContainsSyntheticMedia);
        Assert.False(updatedVideo.Status.SelfDeclaredMadeForKids);
        Assert.True(updatedVideo.Status.Embeddable);
        Assert.Equal("youtube", updatedVideo.Status.License);
        Assert.True(updatedVideo.Status.PublicStatsViewable);
        Assert.Null(updatedVideo.Status.PublishAtDateTimeOffset);
    }

    [Fact]
    public async Task PublishAsync_UpdateFailure_RetainsCreatedBroadcastIdAndDoesNotDelete()
    {
        _client
            .Setup(candidate => candidate.InsertBroadcastAsync(
                It.IsAny<LiveBroadcast>(),
                CancellationToken.None))
            .ReturnsAsync(new LiveBroadcast { Id = BroadcastId });
        _client
            .Setup(candidate => candidate.GetVideoAsync(
                BroadcastId,
                "snippet",
                CancellationToken.None))
            .ReturnsAsync(Video());
        _client
            .Setup(candidate => candidate.UpdateVideoAsync(
                It.IsAny<Video>(),
                "snippet",
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("provider rejected update"));
        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => _publisher.PublishAsync(
                Request(Settings(categoryId: "999999")),
                CancellationToken.None));

        Assert.Equal(BroadcastId, exception.ExternalResourceId);
        _client.Verify(candidate => candidate.UpdateVideoAsync(
            It.IsAny<Video>(),
            "snippet",
            CancellationToken.None));
    }

    [Fact]
    public async Task PublishAsync_CheckpointFailure_StopsLaterMetadataAndCarriesCreatedId()
    {
        _client
            .Setup(candidate => candidate.InsertBroadcastAsync(
                It.IsAny<LiveBroadcast>(),
                CancellationToken.None))
            .ReturnsAsync(new LiveBroadcast { Id = BroadcastId });
        var checkpoint = new Mock<IPlatformPublishCheckpoint>();
        checkpoint
            .Setup(candidate => candidate.SaveExternalResourceIdAsync(
                BroadcastId,
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("storage unavailable"));

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(() =>
            _publisher.PublishAsync(
                Request(Settings(categoryId: "27")),
                checkpoint.Object,
                CancellationToken.None));

        Assert.Equal(BroadcastId, exception.ExternalResourceId);
        checkpoint.Verify(candidate => candidate.SaveExternalResourceIdAsync(
            BroadcastId,
            CancellationToken.None), Times.Once());
        _client.Verify(candidate => candidate.GetVideoAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _client.Verify(candidate => candidate.UpdateVideoAsync(
            It.IsAny<Video>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task PublishAsync_CallerCancellation_PropagatesOriginalCancellation()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        _client
            .Setup(candidate => candidate.InsertBroadcastAsync(
                It.IsAny<LiveBroadcast>(),
                caller.Token))
            .ThrowsAsync(new OperationCanceledException(caller.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _publisher.PublishAsync(
                Request(Settings()),
                caller.Token));
    }

    [Fact]
    public async Task PublishAsync_DependencyTimeout_IsClassified()
    {
        _client
            .Setup(candidate => candidate.InsertBroadcastAsync(
                It.IsAny<LiveBroadcast>(),
                CancellationToken.None))
            .ThrowsAsync(new TaskCanceledException(
                "provider timeout",
                new TimeoutException("deadline")));

        var exception = await Assert.ThrowsAsync<PlatformPublishException>(() =>
            _publisher.PublishAsync(
                Request(Settings()),
                CancellationToken.None));

        Assert.Equal(PlatformPublishFailureKind.Timeout, exception.FailureKind);
    }

    [Fact]
    public async Task PublishAsync_ProviderFailure_ThrowsWithoutLoggingSecrets()
    {
        _client
            .Setup(candidate => candidate.InsertBroadcastAsync(
                It.IsAny<LiveBroadcast>(),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("provider failed"));
        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => _publisher.PublishAsync(
                Request(Settings()),
                CancellationToken.None));

        Assert.Null(exception.ExternalResourceId);
        var logText = _logger.GetLogText();
        Assert.DoesNotContain(ClientSecret, logText);
        Assert.DoesNotContain(RefreshToken, logText);
    }

    [Fact]
    public async Task PublishAsync_UnexpectedOperationCanceledException_IsClassified()
    {
        _client
            .Setup(candidate => candidate.InsertBroadcastAsync(
                It.IsAny<LiveBroadcast>(),
                CancellationToken.None))
            .ThrowsAsync(new OperationCanceledException());
        var exception = await Assert.ThrowsAsync<PlatformPublishException>(
            () => _publisher.PublishAsync(
                Request(Settings()),
                CancellationToken.None));

        Assert.Equal(PlatformPublishFailureKind.UnexpectedCancellation, exception.FailureKind);
    }

    [Fact]
    public async Task PublishAsync_NonYouTubeSettings_Throws()
    {
        await Assert.ThrowsAsync<PlatformPublishException>(
            () => _publisher.PublishAsync(
                Request(new OtherSettings()),
                CancellationToken.None));
    }

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

}
