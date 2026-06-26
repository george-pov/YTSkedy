using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class GetCalendarEventDetailHandlerTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string OtherPlatformId = "8c1d77e0c0a04b2bb0d6f7a9e2c31845";
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNull()
    {
        var handler = new GetCalendarEventDetailHandler(
            new FakeCalendarEventReader(null),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ExistingEvent_ReturnsEventReadModel()
    {
        var calendarEvent = CreateEvent();
        var handler = new GetCalendarEventDetailHandler(
            new FakeCalendarEventReader(calendarEvent),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Same(calendarEvent, result!.Event);
    }

    [Fact]
    public async Task HandleAsync_NoActivePlatforms_ReturnsEmptyPlatforms()
    {
        var handler = new GetCalendarEventDetailHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!.Platforms);
    }

    [Fact]
    public async Task HandleAsync_ActivePlatformWithNoRow_ComputesNotPublished()
    {
        var handler = new GetCalendarEventDetailHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now));

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
            publishedUtc: publishedUtc);
        var handler = new GetCalendarEventDetailHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([publication]),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        var item = Assert.Single(result!.Platforms);
        Assert.Equal(PublishStatus.Published, item.Status);
        Assert.Equal("abc123youtubeid", item.ExternalResourceId);
        Assert.Equal(publishedUtc, item.PublishedUtc);
        Assert.Null(item.PlatformDeletedUtc);
        Assert.False(item.CanPublish);
        Assert.True(item.CanDeletePublication);
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
        var handler = new GetCalendarEventDetailHandler(
            new FakeCalendarEventReader(CreateEvent(
                new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero))),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([publication]),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        var item = Assert.Single(result!.Platforms);
        Assert.False(item.CanPublish);
        Assert.False(item.CanDeletePublication);
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
            platformDeletedUtc: deletedUtc);
        var handler = new GetCalendarEventDetailHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([orphan]),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(2, result!.Platforms.Count);

        var active = result.Platforms.Single(item => item.PlatformId == PlatformId);
        Assert.Equal(PublishStatus.NotPublished, active.Status);
        Assert.True(active.CanPublish);
        Assert.False(active.CanDeletePublication);

        var history = result.Platforms.Single(item => item.PlatformId == OtherPlatformId);
        Assert.Equal("Old channel", history.PlatformName);
        Assert.Equal(PublishStatus.Published, history.Status);
        Assert.Equal("oldyoutubeid", history.ExternalResourceId);
        Assert.Equal(deletedUtc, history.PlatformDeletedUtc);
        Assert.False(history.CanPublish);
        Assert.False(history.CanDeletePublication);
    }

    [Fact]
    public async Task HandleAsync_ReadsCalendarEventExactlyOnce()
    {
        var reader = new FakeCalendarEventReader(CreateEvent());
        var handler = new GetCalendarEventDetailHandler(
            reader,
            new FakePlatformReader([CreatePlatform(PlatformId, "Main channel")]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now));

        await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(1, reader.GetByIdCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_BlankId_Throws(string calendarEventId)
    {
        var handler = new GetCalendarEventDetailHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakePlatformReader([]),
            new FakePlatformPublicationReader([]),
            new FixedTimeProvider(Now));

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
            [new LocalizedDescription("en", "English stream 1", null)]);

    private static PlatformView CreatePlatform(string platformId, string name) =>
        new(platformId, name, PlatformType.YouTube, YouTubeSettings());

    private static YouTubeSettings YouTubeSettings() =>
        new(new YouTubeCredentials("client-id", "client-secret", "refresh-token"), "private", false);

    private static PlatformPublication CreatePublication(
        string platformId,
        string platformName,
        PublishStatus status,
        string? externalResourceId = null,
        DateTimeOffset? publishedUtc = null,
        DateTimeOffset? platformDeletedUtc = null) =>
        new(
            CalendarEventId,
            platformId,
            platformName,
            PlatformType.YouTube,
            status,
            externalResourceId,
            publishedUtc,
            platformDeletedUtc,
            new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero));

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
}
