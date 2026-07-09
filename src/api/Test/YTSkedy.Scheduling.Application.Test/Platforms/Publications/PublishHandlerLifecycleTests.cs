using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerLifecycleTests
{
    [Fact]
    public async Task HandleAsync_AttemptConflict_ReturnsPublishInProgress()
    {
        var repository = new PublishFakePublicationRepository
        {
            StartResult = StartPublicationResult.Conflict
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PublishInProgress, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_ReleasesAttemptAndReturnsProviderFailed()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Throws = new PlatformPublishException("provider down")
        };
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher, repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.ProviderFailed, result.Status);
        Assert.True(repository.ReleaseCalled);
        Assert.False(repository.MarkPublishedCalled);
    }

    [Fact]
    public async Task HandleAsync_FinalizeReturnsNull_ReturnsFinalizeFailed()
    {
        var repository = new PublishFakePublicationRepository { MarkPublishedResult = null };
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher, repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.FinalizeFailed, result.Status);
        Assert.False(repository.ReleaseCalled);
    }

    [Fact]
    public async Task HandleAsync_Success_StartsPublishesFinalizesAndReturnsPublished()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);
        var repository = new PublishFakePublicationRepository
        {
            MarkPublishedResult = publishedUtc
        };
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher, repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal(PlatformId, result.Platform!.PlatformId);
        Assert.Equal("Main YouTube channel", result.Platform.PlatformName);
        Assert.Equal(PlatformType.YouTube, result.Platform.PlatformType);
        Assert.Equal(PublishStatus.Published, result.Platform.Status);
        Assert.Equal("yt-broadcast-id", result.Platform.ExternalResourceId);
        Assert.Equal(ThumbnailPublishStatus.NotConfigured, result.Platform.ThumbnailStatus);
        Assert.Equal(publishedUtc, result.Platform.PublishedUtc);
        Assert.Null(result.Platform.PlatformDeletedUtc);
        Assert.False(result.Platform.CanPublish);
        Assert.True(result.Platform.CanDeletePublication);
        Assert.True(result.Platform.CanPreviewPublishingContent);

        Assert.True(repository.Started);
        Assert.Equal("yt-broadcast-id", repository.MarkedExternalResourceId);
        Assert.False(repository.ReleaseCalled);
        Assert.Equal("English title", repository.StartedAttempt!.ContentSnapshot.Title);
        Assert.Equal("English description", repository.StartedAttempt.ContentSnapshot.Description);

        Assert.Equal("English title", publisher.Request!.Title);
        Assert.Equal("English description", publisher.Request.Description);
        Assert.Equal(FutureStart, publisher.Request.ScheduledStartUtc);
        Assert.Same(YouTubePublishSettings, publisher.Request.PublishSettings);
    }

    [Fact]
    public async Task HandleAsync_WordPressSuccess_ReturnsWordPressPlatformAndPostId()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);
        var repository = new PublishFakePublicationRepository
        {
            MarkPublishedResult = publishedUtc
        };
        var publisher = new PublishFakePublisher(
            PlatformType.WordPress,
            new PlatformPublishResult("123"));
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(
                "Company blog",
                PlatformType.WordPress,
                WordPressPublishSettings),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal("Company blog", result.Platform!.PlatformName);
        Assert.Equal(PlatformType.WordPress, result.Platform.PlatformType);
        Assert.Equal(publishedUtc, result.Platform.PublishedUtc);
        Assert.Equal("123", result.Platform.ExternalResourceId);
        Assert.Null(result.Platform.ThumbnailStatus);
        Assert.False(result.Platform.CanPublish);
        Assert.True(result.Platform.CanDeletePublication);
        Assert.True(result.Platform.CanPreviewPublishingContent);

        Assert.Equal("123", repository.MarkedExternalResourceId);
        Assert.Same(WordPressPublishSettings, publisher.Request!.PublishSettings);
    }
}
