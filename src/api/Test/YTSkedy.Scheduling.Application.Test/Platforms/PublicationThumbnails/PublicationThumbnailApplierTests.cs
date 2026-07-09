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
        var writer = new FakePublicationThumbnailWriter();
        var publisher = new FakeThumbnailPublisher();
        var content = ApplicationTestData.ThumbnailContent();
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(content)),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Applied, result);
        Assert.True(writer.MarkThumbnailAppliedCalled);
        Assert.False(writer.MarkThumbnailFailedCalled);
        Assert.Equal(ExternalResourceId, publisher.Request!.ExternalResourceId);
        Assert.Same(content, publisher.Request.ThumbnailContent);
    }

    [Fact]
    public async Task ApplyAsync_YouTubeThumbnailFailure_MarksThumbnailFailed()
    {
        var writer = new FakePublicationThumbnailWriter();
        var publisher = new FakeThumbnailPublisher
        {
            Throws = new ThumbnailPublishException("thumbnail rejected")
        };
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent())),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        Assert.False(writer.MarkThumbnailAppliedCalled);
        Assert.True(writer.MarkThumbnailFailedCalled);
    }

    [Fact]
    public async Task ApplyAsync_UnexpectedThumbnailFailure_MarksThumbnailFailed()
    {
        var writer = new FakePublicationThumbnailWriter();
        var publisher = new FakeThumbnailPublisher
        {
            Throws = new InvalidOperationException("thumbnail client failed")
        };
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent())),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        Assert.False(writer.MarkThumbnailAppliedCalled);
        Assert.True(writer.MarkThumbnailFailedCalled);
    }

    [Fact]
    public async Task ApplyAsync_ConfiguredThumbnailWithMissingBytes_MarksThumbnailFailed()
    {
        var writer = new FakePublicationThumbnailWriter();
        var publisher = new FakeThumbnailPublisher();
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(PublicationThumbnail.MissingContent),
            CancellationToken.None);

        Assert.Equal(ThumbnailPublishStatus.Failed, result);
        Assert.True(writer.MarkThumbnailFailedCalled);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task ApplyAsync_WordPressWithEventThumbnail_ReturnsNullWithoutProviderCall()
    {
        var writer = new FakePublicationThumbnailWriter();
        var publisher = new FakeThumbnailPublisher();
        var applier = CreateApplier(writer: writer, publisher: publisher);

        var result = await applier.ApplyAsync(
            Command(
                PublicationThumbnail.Configured(ApplicationTestData.ThumbnailContent()),
                PlatformType.WordPress,
                ApplicationTestData.WordPressSettings()),
            CancellationToken.None);

        Assert.Null(result);
        Assert.False(writer.MarkThumbnailAppliedCalled);
        Assert.False(writer.MarkThumbnailFailedCalled);
        Assert.Null(publisher.Request);
    }

    private static PublicationThumbnailApplier CreateApplier(
        Thumbnail? thumbnail = null,
        ThumbnailContent? thumbnailContent = null,
        FakePublicationThumbnailWriter? writer = null,
        IThumbnailPublisher? publisher = null) =>
        new(
            new FakeThumbnailReader(thumbnail),
            new FakeThumbnailStore(thumbnailContent),
            writer ?? new FakePublicationThumbnailWriter(),
            new PlatformTypeAdapterSelector<IThumbnailPublisher>(
                publisher is null ? [] : [publisher]),
            NullLogger<PublicationThumbnailApplier>.Instance);

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

    private sealed class FakePublicationThumbnailWriter : IPublicationThumbnailWriter
    {
        public bool MarkThumbnailAppliedCalled { get; private set; }

        public bool MarkThumbnailFailedCalled { get; private set; }

        public Task<bool> MarkThumbnailAppliedAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken)
        {
            MarkThumbnailAppliedCalled = true;

            return Task.FromResult(true);
        }

        public Task<bool> MarkThumbnailFailedAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken)
        {
            MarkThumbnailFailedCalled = true;

            return Task.FromResult(true);
        }
    }

    private sealed class FakeThumbnailPublisher : IThumbnailPublisher
    {
        public PlatformType Type => PlatformType.YouTube;

        public Exception? Throws { get; init; }

        public ThumbnailPublishRequest? Request { get; private set; }

        public Task PublishAsync(
            ThumbnailPublishRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;

            if (Throws is not null)
            {
                throw Throws;
            }

            return Task.CompletedTask;
        }
    }
}
