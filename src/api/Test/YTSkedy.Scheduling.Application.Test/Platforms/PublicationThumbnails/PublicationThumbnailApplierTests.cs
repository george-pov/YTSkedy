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
    private readonly Mock<ICalendarEventThumbnailReader> _thumbnails = new();
    private readonly Mock<IThumbnailStore> _store = new();
    private readonly Mock<IPublicationThumbnailWriter> _writer = new();
    private readonly Mock<IThumbnailPublisher> _publisher = new();
    private readonly PublicationThumbnailApplier _applier;

    public PublicationThumbnailApplierTests()
    {
        _publisher.SetupGet(candidate => candidate.Type).Returns(PlatformType.YouTube);
        _applier = new PublicationThumbnailApplier(
            _thumbnails.Object,
            _store.Object,
            _writer.Object,
            new PlatformTypeAdapterSelector<IThumbnailPublisher>([_publisher.Object]),
            NullLogger<PublicationThumbnailApplier>.Instance);
    }

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
        var publisher = ThumbnailPublisher();
        var content = ApplicationTestData.ThumbnailContent();
        var applier = CreateApplier();

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(content)),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Applied, result);
        _writer.Verify(candidate => candidate.MarkThumbnailAppliedAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
        _writer.Verify(candidate => candidate.MarkThumbnailFailedAsync(
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
        ThumbnailPublisher(
            new ThumbnailPublishException("thumbnail rejected"));
        var applier = CreateApplier();

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent())),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        VerifyFailed();
    }

    [Fact]
    public async Task ApplyAsync_UnexpectedThumbnailFailure_MarksThumbnailFailed()
    {
        ThumbnailPublisher(
            new InvalidOperationException("thumbnail client failed"));
        var applier = CreateApplier();

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent())),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        VerifyFailed();
    }

    [Fact]
    public async Task ApplyAsync_ConfiguredThumbnailWithMissingBytes_MarksThumbnailFailed()
    {
        var publisher = ThumbnailPublisher();
        var applier = CreateApplier();

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.MissingContent),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        _writer.Verify(candidate => candidate.MarkThumbnailFailedAsync(
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
        var publisher = ThumbnailPublisher();
        var applier = CreateApplier();

        var result = await applier.ApplyAsync(
            Command(
                PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent()),
                PlatformType.WordPress,
                ApplicationTestData.WordPressSettings()),
            CancellationToken.None);

        Assert.Null(result);
        _writer.Verify(candidate => candidate.MarkThumbnailAppliedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _writer.Verify(candidate => candidate.MarkThumbnailFailedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        publisher.Verify(candidate => candidate.PublishAsync(
            It.IsAny<ThumbnailPublishRequest>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    private PublicationThumbnailApplier CreateApplier(
        Thumbnail? thumbnail = null,
        ThumbnailContent? thumbnailContent = null)
    {
        _thumbnails
            .Setup(candidate => candidate.GetThumbnailAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(thumbnail);
        if (thumbnail is not null)
        {
            _store
                .Setup(candidate => candidate.GetAsync(
                    thumbnail.BlobName,
                    CancellationToken.None))
                .ReturnsAsync(thumbnailContent);
        }
        _writer
            .Setup(candidate => candidate.MarkThumbnailAppliedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _writer
            .Setup(candidate => candidate.MarkThumbnailFailedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return _applier;
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

    private Mock<IThumbnailPublisher> ThumbnailPublisher(Exception? exception = null)
    {
        var setup = _publisher.Setup(candidate => candidate.PublishAsync(
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

        return _publisher;
    }

    private void VerifyFailed()
    {
        _writer.Verify(candidate => candidate.MarkThumbnailAppliedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _writer.Verify(candidate => candidate.MarkThumbnailFailedAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
    }
}
