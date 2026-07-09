using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class GetCalendarEventDetailsHandlerTests
{
    private const string CalendarEventId = GetCalendarEventDetailsScenario.CalendarEventId;
    private const string PlatformId = ApplicationTestData.PlatformId;
    private const string OtherPlatformId = ApplicationTestData.OtherPlatformId;

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNull()
    {
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = null
        };

        var result = await scenario.HandleAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ExistingEvent_ReturnsEventReadModel()
    {
        var calendarEvent = CreateEvent();
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = calendarEvent
        };

        var result = await scenario.HandleAsync();

        Assert.NotNull(result);
        Assert.Same(calendarEvent, result!.Event);
    }

    [Fact]
    public async Task HandleAsync_NoPublicationRows_AllowsUpdateAndDelete()
    {
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent(),
            Platforms = [CreatePlatform(PlatformId, "Main channel")]
        };

        var result = await scenario.HandleAsync();

        Assert.NotNull(result);
        Assert.True(result!.CanUpdate);
        Assert.True(result.CanDelete);
        Assert.True(result.CanUpdateThumbnail);
    }

    [Fact]
    public async Task HandleAsync_PublicationRowsExist_DisallowsUpdateAndDelete()
    {
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent(),
            Platforms = [CreatePlatform(PlatformId, "Main channel")],
            Publications =
            [
                CreatePublication(
                    PlatformId,
                    "Main channel",
                    PublishStatus.Published,
                    externalResourceId: "abc123youtubeid")
            ]
        };

        var result = await scenario.HandleAsync();

        Assert.NotNull(result);
        Assert.False(result!.CanUpdate);
        Assert.False(result.CanDelete);
        Assert.False(result.CanUpdateThumbnail);
    }

    [Fact]
    public async Task HandleAsync_ExistingThumbnail_ReturnsThumbnail()
    {
        var thumbnail = CreateThumbnail();
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent(),
            Thumbnail = thumbnail
        };

        var result = await scenario.HandleAsync();

        Assert.Same(thumbnail, result!.Thumbnail);
    }

    [Fact]
    public async Task HandleAsync_NoActivePlatforms_ReturnsEmptyPlatforms()
    {
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent()
        };

        var result = await scenario.HandleAsync();

        Assert.NotNull(result);
        Assert.Empty(result!.Platforms);
    }

    [Fact]
    public async Task HandleAsync_ActivePlatformWithNoRow_ComputesNotPublished()
    {
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent(),
            Platforms = [CreatePlatform(PlatformId, "Main channel")]
        };

        var result = await scenario.HandleAsync();

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
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent(),
            Platforms = [CreatePlatform(PlatformId, "Main channel")],
            Publications = [publication]
        };

        var result = await scenario.HandleAsync();

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
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)),
            Platforms = [CreatePlatform(PlatformId, "Main channel")],
            Publications = [publication]
        };

        var result = await scenario.HandleAsync();

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
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent(),
            Platforms = [CreatePlatform(PlatformId, "Main channel")],
            Publications = [orphan]
        };

        var result = await scenario.HandleAsync();

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
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent(),
            Platforms = [CreatePlatform(PlatformId, "Main channel")]
        };

        await scenario.HandleAsync();

        Assert.Equal(1, scenario.CalendarEventReader.GetByIdCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_BlankId_Throws(string calendarEventId)
    {
        var scenario = new GetCalendarEventDetailsScenario
        {
            CalendarEvent = CreateEvent()
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => scenario.HandleAsync(calendarEventId));
    }

    private static CalendarEventView CreateEvent() =>
        CreateEvent(new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero));

    private static CalendarEventView CreateEvent(DateTimeOffset scheduledStartUtc) =>
        ApplicationTestData.CalendarEvent(
            calendarEventId: CalendarEventId,
            start: new ScheduledStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            scheduledStartUtc: scheduledStartUtc,
            text: EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "English stream 1"),
                    new EventTextValue("text2", "Event details")
                ]));

    private static PlatformView CreatePlatform(string platformId, string name) =>
        ApplicationTestData.Platform(platformId: platformId, name: name);

    private static PlatformPublication CreatePublication(
        string platformId,
        string platformName,
        PublishStatus status,
        string? externalResourceId = null,
        DateTimeOffset? publishedUtc = null,
        DateTimeOffset? platformDeletedUtc = null,
        ContentSnapshot? contentSnapshot = null) =>
        ApplicationTestData.Publication(
            status,
            calendarEventId: CalendarEventId,
            platformId: platformId,
            platformName: platformName,
            externalResourceId: externalResourceId,
            publishedUtc: publishedUtc,
            platformDeletedUtc: platformDeletedUtc,
            contentSnapshot: contentSnapshot);

    private static Thumbnail CreateThumbnail() =>
        ApplicationTestData.Thumbnail(
            calendarEventId: CalendarEventId,
            updatedUtc: new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero));
}
