using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class PublicationThumbnailApplierTests
{
    private const string CalendarEventId = ApplicationTestData.CalendarEventId;
    private const string PlatformId = ApplicationTestData.PlatformId;
    private const string ExternalResourceId = "yt-broadcast-id";

    [Fact]
    public async Task LoadAsync_MissingThumbnail_ReturnsNotConfigured()
    {
        var applier = CreateApplier(thumbnail: null);

        var result = await applier.LoadAsync(CalendarEventId, CancellationToken.None);

        Assert.False(result.IsConfigured);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task LoadAsync_ConfiguredThumbnailWithoutBytes_ReturnsMissingContent()
    {
        var applier = CreateApplier(
            thumbnail: ApplicationTestData.Thumbnail(calendarEventId: CalendarEventId),
            thumbnailContent: null);

        var result = await applier.LoadAsync(CalendarEventId, CancellationToken.None);

        Assert.True(result.IsConfigured);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task ApplyAsync_YouTubeThumbnailSuccess_MarksThumbnailApplied()
    {
        var writer = new Mock<IPublicationThumbnailWriter>();
        var publisher = ThumbnailPublisher();
        var content = ApplicationTestData.ThumbnailContent();
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(content)),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Applied, result);
        writer.Verify(candidate => candidate.MarkThumbnailAppliedAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
        writer.Verify(candidate => candidate.MarkThumbnailFailedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        publisher.Verify(candidate => candidate.PublishAsync(
            It.Is<ThumbnailPublishRequest>(request =>
                request.ExternalResourceId == ExternalResourceId &&
                ReferenceEquals(request.ThumbnailContent, content)),
            CancellationToken.None));
    }

    [Fact]
    public async Task ApplyAsync_YouTubeThumbnailFailure_MarksThumbnailFailed()
    {
        var writer = new Mock<IPublicationThumbnailWriter>();
        var publisher = ThumbnailPublisher(
            new ThumbnailPublishException("thumbnail rejected"));
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent())),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        VerifyFailed(writer);
    }

    [Fact]
    public async Task ApplyAsync_UnexpectedThumbnailFailure_MarksThumbnailFailed()
    {
        var writer = new Mock<IPublicationThumbnailWriter>();
        var publisher = ThumbnailPublisher(
            new InvalidOperationException("thumbnail client failed"));
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent())),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        VerifyFailed(writer);
    }

    [Fact]
    public async Task ApplyAsync_ConfiguredThumbnailWithMissingBytes_MarksThumbnailFailed()
    {
        var writer = new Mock<IPublicationThumbnailWriter>();
        var publisher = ThumbnailPublisher();
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.MissingContent),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        writer.Verify(candidate => candidate.MarkThumbnailFailedAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
        publisher.Verify(candidate => candidate.PublishAsync(
            It.IsAny<ThumbnailPublishRequest>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task ApplyAsync_WordPressWithEventThumbnail_ReturnsNullWithoutProviderCall()
    {
        var writer = new Mock<IPublicationThumbnailWriter>();
        var publisher = ThumbnailPublisher();
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(
                PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent()),
                PlatformType.WordPress,
                ApplicationTestData.WordPressSettings()),
            CancellationToken.None);

        Assert.Null(result);
        writer.Verify(candidate => candidate.MarkThumbnailAppliedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        writer.Verify(candidate => candidate.MarkThumbnailFailedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        publisher.Verify(candidate => candidate.PublishAsync(
            It.IsAny<ThumbnailPublishRequest>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    private static PublicationThumbnailApplier CreateApplier(
        Thumbnail? thumbnail = null,
        ThumbnailContent? thumbnailContent = null,
        Mock<IPublicationThumbnailWriter>? writer = null,
        Mock<IThumbnailPublisher>? publisher = null)
    {
        var thumbnailReader = new Mock<ICalendarEventThumbnailReader>();
        thumbnailReader
            .Setup(candidate => candidate.GetThumbnailAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(thumbnail);
        var thumbnailStore = new Mock<IThumbnailStore>();
        if (thumbnail is not null)
        {
            thumbnailStore
                .Setup(candidate => candidate.GetAsync(
                    thumbnail.BlobName,
                    CancellationToken.None))
                .ReturnsAsync(thumbnailContent);
        }
        writer ??= new Mock<IPublicationThumbnailWriter>();
        writer
            .Setup(candidate => candidate.MarkThumbnailAppliedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        writer
            .Setup(candidate => candidate.MarkThumbnailFailedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return new PublicationThumbnailApplier(
            thumbnailReader.Object,
            thumbnailStore.Object,
            writer.Object,
            new PlatformTypeAdapterSelector<IThumbnailPublisher>(
                publisher is null ? [] : [publisher.Object]),
            NullLogger<PublicationThumbnailApplier>.Instance);
    }

    private static PublicationThumbnailCommand Command(
        PublicationThumbnail thumbnail,
        PlatformType platformType = PlatformType.YouTube,
        PublishSettings? publishSettings = null) =>
        new(
            CalendarEventId,
            PlatformId,
            ApplicationTestData.Platform(
                platformId: PlatformId,
                type: platformType,
                publishSettings: publishSettings),
            ExternalResourceId,
            thumbnail);

    private static Mock<IThumbnailPublisher> ThumbnailPublisher(Exception? exception = null)
    {
        var publisher = new Mock<IThumbnailPublisher>();
        publisher.SetupGet(candidate => candidate.Type).Returns(PlatformType.YouTube);
        var setup = publisher.Setup(candidate => candidate.PublishAsync(
            It.IsAny<ThumbnailPublishRequest>(),
            It.IsAny<CancellationToken>()));
        if (exception is null)
        {
            setup.Returns(Task.CompletedTask);
        }
        else
        {
            setup.ThrowsAsync(exception);
        }

        return publisher;
    }

    private static void VerifyFailed(Mock<IPublicationThumbnailWriter> writer)
    {
        writer.Verify(candidate => candidate.MarkThumbnailAppliedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        writer.Verify(candidate => candidate.MarkThumbnailFailedAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
    }
}
