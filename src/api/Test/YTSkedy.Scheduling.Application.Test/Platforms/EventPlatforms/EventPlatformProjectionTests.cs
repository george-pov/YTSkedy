using YTSkedy.Scheduling.Application.Platforms.EventPlatforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class EventPlatformProjectionTests
{
    private const string CalendarEventId = ApplicationTestData.CalendarEventId;
    private const string PlatformId = ApplicationTestData.PlatformId;
    private const string OtherPlatformId = ApplicationTestData.OtherPlatformId;

    private static readonly DateTimeOffset Now =
        new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Project_ActivePlatformWithoutRow_ComputesNotPublished()
    {
        var calendarEvent = CreateEvent();
        var platform = CreatePlatform(PlatformId, "Main channel");

        var result = EventPlatformProjection.Project(calendarEvent, [platform], [], Now);

        var item = Assert.Single(result);
        Assert.Equal(PlatformId, item.PlatformId);
        Assert.Equal("Main channel", item.PlatformName);
        Assert.Equal(PublishStatus.NotPublished, item.Status);
        Assert.Null(item.ExternalResourceId);
        Assert.True(item.CanPublish);
        Assert.False(item.CanDeletePublication);
        Assert.True(item.CanPreviewPublishingContent);
        Assert.Equal(ThumbnailPublishStatus.NotConfigured, item.ThumbnailStatus);
    }

    [Fact]
    public void Project_ActivePublishedRow_ReportsResourceAndDeleteEligibility()
    {
        var calendarEvent = CreateEvent();
        var platform = CreatePlatform(PlatformId, "Main channel");
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var publication = CreatePublication(
            PlatformId,
            "Main channel",
            PublishStatus.Published,
            externalResourceId: "abc123youtubeid",
            publishedUtc: publishedUtc,
            contentSnapshot: new ContentSnapshot("Rendered title", "Rendered description"));

        var result = EventPlatformProjection.Project(
            calendarEvent,
            [platform],
            [publication],
            Now);

        var item = Assert.Single(result);
        Assert.Equal(PublishStatus.Published, item.Status);
        Assert.Equal("abc123youtubeid", item.ExternalResourceId);
        Assert.Equal(publishedUtc, item.PublishedUtc);
        Assert.False(item.CanPublish);
        Assert.True(item.CanDeletePublication);
        Assert.True(item.CanPreviewPublishingContent);
    }

    [Fact]
    public void Project_PastPublishedRow_CannotDeletePublication()
    {
        var calendarEvent = CreateEvent(Now);
        var platform = CreatePlatform(PlatformId, "Main channel");
        var publication = CreatePublication(
            PlatformId,
            "Main channel",
            PublishStatus.Published,
            externalResourceId: "abc123youtubeid",
            publishedUtc: new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero),
            contentSnapshot: new ContentSnapshot("Rendered title", "Rendered description"));

        var result = EventPlatformProjection.Project(
            calendarEvent,
            [platform],
            [publication],
            Now);

        var item = Assert.Single(result);
        Assert.False(item.CanPublish);
        Assert.False(item.CanDeletePublication);
        Assert.True(item.CanPreviewPublishingContent);
    }

    [Fact]
    public void Project_OrphanRowForDeletedPlatform_AppendsReadOnlyHistory()
    {
        var calendarEvent = CreateEvent();
        var activePlatform = CreatePlatform(PlatformId, "Main channel");
        var deletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);
        var orphan = CreatePublication(
            OtherPlatformId,
            "Old channel",
            PublishStatus.Published,
            externalResourceId: "oldyoutubeid",
            publishedUtc: new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero),
            platformDeletedUtc: deletedUtc,
            contentSnapshot: new ContentSnapshot("Rendered title", null));

        var result = EventPlatformProjection.Project(
            calendarEvent,
            [activePlatform],
            [orphan],
            Now);

        Assert.Equal(2, result.Count);

        var history = result.Single(item => item.PlatformId == OtherPlatformId);
        Assert.Equal("Old channel", history.PlatformName);
        Assert.Equal(PublishStatus.Published, history.Status);
        Assert.Equal("oldyoutubeid", history.ExternalResourceId);
        Assert.Equal(deletedUtc, history.PlatformDeletedUtc);
        Assert.False(history.CanPublish);
        Assert.False(history.CanDeletePublication);
        Assert.True(history.CanPreviewPublishingContent);
    }

    [Fact]
    public void ProjectNotPublished_WordPressPlatform_HasNoThumbnailStatus()
    {
        var calendarEvent = CreateEvent();
        var platform = CreatePlatform(
            PlatformId,
            "Company blog",
            PlatformType.WordPress,
            ApplicationTestData.WordPressSettings());

        var result = EventPlatformProjection.ProjectNotPublished(
            calendarEvent,
            platform,
            Now);

        Assert.Equal(PublishStatus.NotPublished, result.Status);
        Assert.Null(result.ThumbnailStatus);
    }

    [Fact]
    public void ProjectPublished_FutureEvent_ReturnsPublishedActionFlags()
    {
        var calendarEvent = CreateEvent();
        var platform = CreatePlatform(PlatformId, "Main channel");
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

        var result = EventPlatformProjection.ProjectPublished(
            calendarEvent,
            platform,
            "abc123youtubeid",
            publishedUtc,
            Now,
            ThumbnailPublishStatus.Applied);

        Assert.Equal(PublishStatus.Published, result.Status);
        Assert.Equal("abc123youtubeid", result.ExternalResourceId);
        Assert.Equal(publishedUtc, result.PublishedUtc);
        Assert.False(result.CanPublish);
        Assert.True(result.CanDeletePublication);
        Assert.True(result.CanPreviewPublishingContent);
        Assert.Equal(ThumbnailPublishStatus.Applied, result.ThumbnailStatus);
    }

    private static CalendarEventView CreateEvent(DateTimeOffset? scheduledStartUtc = null) =>
        ApplicationTestData.CalendarEvent(
            calendarEventId: CalendarEventId,
            start: new ScheduledStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            scheduledStartUtc: scheduledStartUtc ??
                new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            text: ApplicationTestData.Text());

    private static PlatformView CreatePlatform(
        string platformId,
        string name,
        PlatformType type = PlatformType.YouTube,
        PublishSettings? publishSettings = null) =>
        ApplicationTestData.Platform(
            platformId: platformId,
            name: name,
            type: type,
            publishSettings: publishSettings);

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
}
