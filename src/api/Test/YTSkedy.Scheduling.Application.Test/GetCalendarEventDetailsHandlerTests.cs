using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class GetCalendarEventDetailsHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string OtherPlatformId = "8c1d77e0c0a04b2bb0d6f7a9e2c31845";
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNull()
    {
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(null),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ExistingEvent_ReturnsEventReadModel()
    {
        var calendarEvent = CreateEvent();
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(calendarEvent),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Same(calendarEvent, result!.Event);
    }

    [Fact]
    public async Task HandleAsync_NoPublicationRows_AllowsUpdateAndDelete()
    {
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.CanUpdate);
        Assert.True(result.CanDelete);
        Assert.True(result.CanUpdateThumbnail);
    }

    [Fact]
    public async Task HandleAsync_PublicationRowsExist_DisallowsUpdateAndDelete()
    {
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([
                CreatePublication(
                    PlatformId,
                    "Main channel",
                    PublishStatus.Published,
                    externalResourceId: "abc123youtubeid")
            ]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result!.CanUpdate);
        Assert.False(result.CanDelete);
        Assert.False(result.CanUpdateThumbnail);
    }

    [Fact]
    public async Task HandleAsync_ExistingThumbnail_ReturnsThumbnail()
    {
        var thumbnail = CreateThumbnail();
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(thumbnail));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Same(thumbnail, result!.Thumbnail);
    }

    [Fact]
    public async Task HandleAsync_NoActivePlatforms_ReturnsEmptyPlatforms()
    {
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.Platforms);
    }

    [Fact]
    public async Task HandleAsync_ActivePlatformWithNoRow_ComputesNotPublished()
    {
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        var item = Assert.Single(result!.Platforms);
        Assert.Equal(PlatformId, item.PlatformId);
        Assert.Equal("Main channel", item.PlatformName);
        Assert.Equal(PlatformType.YouTube, item.PlatformType);
        Assert.Equal(PublishStatus.NotPublished, item.Status);
        Assert.Null(item.ExternalResourceId);
        Assert.Null(item.PublishedUtc);
        Assert.Null(item.PlatformDeletedUtc);
        Assert.True(item.CanPublish);
        Assert.False(item.CanDeletePublication);
        Assert.True(item.CanPreviewPublishingContent);
    }

    [Fact]
    public async Task HandleAsync_ActivePlatformWithPublishedRow_ReportsResourceAndCannotPublish()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var publication = CreatePublication(
            PlatformId,
            "Main channel",
            PublishStatus.Published,
            externalResourceId: "abc123youtubeid",
            publishedUtc: publishedUtc,
            contentSnapshot: new ContentSnapshot("Rendered title", "Rendered description"));
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([publication]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        var item = Assert.Single(result!.Platforms);
        Assert.Equal(PublishStatus.Published, item.Status);
        Assert.Equal("abc123youtubeid", item.ExternalResourceId);
        Assert.Equal(publishedUtc, item.PublishedUtc);
        Assert.Null(item.PlatformDeletedUtc);
        Assert.False(item.CanPublish);
        Assert.True(item.CanDeletePublication);
        Assert.True(item.CanPreviewPublishingContent);
    }

    [Fact]
    public async Task HandleAsync_ActivePlatformWithPastPublishedRow_CannotDeletePublication()
    {
        var publication = CreatePublication(
            PlatformId,
            "Main channel",
            PublishStatus.Published,
            externalResourceId: "abc123youtubeid",
            publishedUtc: new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero));
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent(
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero))),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([publication]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        var item = Assert.Single(result!.Platforms);
        Assert.False(item.CanPublish);
        Assert.False(item.CanDeletePublication);
        Assert.False(item.CanPreviewPublishingContent);
    }

    [Fact]
    public async Task HandleAsync_OrphanRowForDeletedPlatform_IncludedAsReadOnlyHistory()
    {
        var deletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);
        var orphan = CreatePublication(
            OtherPlatformId,
            "Old channel",
            PublishStatus.Published,
            externalResourceId: "oldyoutubeid",
            publishedUtc: new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero),
            platformDeletedUtc: deletedUtc,
            contentSnapshot: new ContentSnapshot("Rendered title", null));
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([orphan]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(2, result!.Platforms.Count);

        var active = result.Platforms.Single(item => item.PlatformId == PlatformId);
        Assert.Equal(PublishStatus.NotPublished, active.Status);
        Assert.True(active.CanPublish);
        Assert.False(active.CanDeletePublication);
        Assert.True(active.CanPreviewPublishingContent);

        var history = result.Platforms.Single(item => item.PlatformId == OtherPlatformId);
        Assert.Equal("Old channel", history.PlatformName);
        Assert.Equal(PublishStatus.Published, history.Status);
        Assert.Equal("oldyoutubeid", history.ExternalResourceId);
        Assert.Equal(deletedUtc, history.PlatformDeletedUtc);
        Assert.False(history.CanPublish);
        Assert.False(history.CanDeletePublication);
        Assert.True(history.CanPreviewPublishingContent);
    }

    [Fact]
    public async Task HandleAsync_ReadsCalendarEventExactlyOnce()
    {
        var reader = new FakeCalendarEventReader(CreateEvent());
        var handler = new GetCalendarEventDetailsHandler(
            reader,
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(1, reader.GetByIdCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_BlankId_Throws(string calendarEventId)
    {
        var handler = new GetCalendarEventDetailsHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now),
            new FakeThumbnailReader(null));

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync(calendarEventId, CancellationToken.None));
    }

    private static CalendarEventView CreateEvent() =>
        CreateEvent(new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero));

    private static CalendarEventView CreateEvent(DateTimeOffset scheduledStartUtc) =>
        new(
            CalendarEventId,
            new ScheduledStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            scheduledStartUtc,
            EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "English stream 1"),
                    new EventTextValue("text2", "Event details")
                ]));

    private static PlatformView CreatePlatform(string platformId, string name) =>
        new(
            platformId,
            name,
            null,
            PlatformType.YouTube,
            YouTubeSettings(),
            RequiredPublishingContent());

    private static YouTubeSettings YouTubeSettings() =>
        new(new YouTubeCredentials("client-id", "client-secret", "refresh-token"), "private", false);

    private static PublishingContent RequiredPublishingContent() =>
        new("title-template", "description-template");

    private static PlatformPublication CreatePublication(
        string platformId,
        string platformName,
        PublishStatus status,
        string? externalResourceId = null,
        DateTimeOffset? publishedUtc = null,
        DateTimeOffset? platformDeletedUtc = null,
        ContentSnapshot? contentSnapshot = null) =>
        new(
            CalendarEventId,
            platformId,
            platformName,
            PlatformType.YouTube,
            status,
            externalResourceId,
            publishedUtc,
            platformDeletedUtc,
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            TargetSnapshot: null,
            ContentSnapshot: contentSnapshot);

    private static Thumbnail CreateThumbnail() =>
        new(
            "stream.png",
            "image/png",
            123,
            1280,
            720,
            new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero),
            $"calendar-events/{CalendarEventId}/thumbnail");

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeCalendarEventReader(CalendarEventView? calendarEvent)
        : ICalendarEventReader
    {
        public int GetByIdCallCount { get; private set; }

        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            GetByIdCallCount++;

            return Task.FromResult(calendarEvent);
        }
    }

    private sealed class FakePlatformReader(IReadOnlyList<PlatformView> platforms) : IPlatformReader
    {
        public Task<IReadOnlyList<PlatformView>> ListAsync(
            PlatformType? type,
            CancellationToken cancellationToken) =>
            Task.FromResult(platforms);

        public Task<PlatformView?> GetAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePlatformPublicationReader(IReadOnlyList<PlatformPublication> publications)
        : IPlatformPublicationReader
    {
        public Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(publications);

        public Task<bool> HasAnyForEventAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(publications.Count > 0);

        public Task<PlatformPublication?> GetAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeThumbnailReader(Thumbnail? thumbnail) : ICalendarEventThumbnailReader
    {
        public Task<Thumbnail?> GetThumbnailAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(thumbnail);
    }
}
