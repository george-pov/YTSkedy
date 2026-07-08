using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerThumbnailTests
{
    [Fact]
    public async Task HandleAsync_YouTubeThumbnailSuccess_MarksThumbnailApplied()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var thumbnailPublisher = new PublishFakeThumbnailPublisher();
        var content = ThumbnailContent();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            thumbnail: Thumbnail(),
            thumbnailContent: content,
            thumbnailPublisher: thumbnailPublisher);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal(ThumbnailPublishStatus.Applied, result.Platform!.ThumbnailStatus);
        Assert.True(repository.MarkThumbnailAppliedCalled);
        Assert.False(repository.MarkThumbnailFailedCalled);
        Assert.False(repository.ReleaseCalled);
        Assert.Equal("yt-broadcast-id", thumbnailPublisher.Request!.ExternalResourceId);
        Assert.Same(YouTubePublishSettings, thumbnailPublisher.Request.PublishSettings);
        Assert.Same(content, thumbnailPublisher.Request.ThumbnailContent);
    }

    [Fact]
    public async Task HandleAsync_YouTubeThumbnailFailure_MarksThumbnailFailedAndKeepsPublished()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var thumbnailPublisher = new PublishFakeThumbnailPublisher
        {
            Throws = new ThumbnailPublishException("thumbnail rejected")
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            thumbnail: Thumbnail(),
            thumbnailContent: ThumbnailContent(),
            thumbnailPublisher: thumbnailPublisher);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal(PublishStatus.Published, result.Platform!.Status);
        Assert.Equal("yt-broadcast-id", result.Platform.ExternalResourceId);
        Assert.Equal(ThumbnailPublishStatus.Failed, result.Platform.ThumbnailStatus);
        Assert.True(repository.MarkPublishedCalled);
        Assert.False(repository.ReleaseCalled);
        Assert.False(repository.MarkThumbnailAppliedCalled);
        Assert.True(repository.MarkThumbnailFailedCalled);
    }

    [Fact]
    public async Task HandleAsync_YouTubeThumbnailUnexpectedFailure_MarksThumbnailFailedAndKeepsPublished()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var thumbnailPublisher = new PublishFakeThumbnailPublisher
        {
            Throws = new InvalidOperationException("thumbnail client failed")
        };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            thumbnail: Thumbnail(),
            thumbnailContent: ThumbnailContent(),
            thumbnailPublisher: thumbnailPublisher);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal(PublishStatus.Published, result.Platform!.Status);
        Assert.Equal(ThumbnailPublishStatus.Failed, result.Platform.ThumbnailStatus);
        Assert.True(repository.MarkPublishedCalled);
        Assert.False(repository.ReleaseCalled);
        Assert.False(repository.MarkThumbnailAppliedCalled);
        Assert.True(repository.MarkThumbnailFailedCalled);
    }

    [Fact]
    public async Task HandleAsync_ConfiguredThumbnailWithMissingBytes_MarksThumbnailFailed()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher
        {
            Result = new PlatformPublishResult("yt-broadcast-id")
        };
        var thumbnailPublisher = new PublishFakeThumbnailPublisher();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            publisher,
            repository: repository,
            thumbnail: Thumbnail(),
            thumbnailContent: null,
            thumbnailPublisher: thumbnailPublisher);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal(ThumbnailPublishStatus.Failed, result.Platform!.ThumbnailStatus);
        Assert.True(repository.MarkThumbnailFailedCalled);
        Assert.Null(thumbnailPublisher.Request);
    }

    [Fact]
    public async Task HandleAsync_WordPressWithEventThumbnail_DoesNotApplyThumbnail()
    {
        var repository = new PublishFakePublicationRepository();
        var publisher = new PublishFakePublisher(
            PlatformType.WordPress,
            new PlatformPublishResult("123"));
        var thumbnailPublisher = new PublishFakeThumbnailPublisher();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(
                "Company blog",
                PlatformType.WordPress,
                WordPressPublishSettings),
            publisher,
            repository: repository,
            thumbnail: Thumbnail(),
            thumbnailContent: ThumbnailContent(),
            thumbnailPublisher: thumbnailPublisher);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Null(result.Platform!.ThumbnailStatus);
        Assert.False(repository.MarkThumbnailAppliedCalled);
        Assert.False(repository.MarkThumbnailFailedCalled);
        Assert.Null(thumbnailPublisher.Request);
    }
}
