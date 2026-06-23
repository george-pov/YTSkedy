using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class GetCalendarEventDetailResponseTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string OrphanPlatformId = "8c1d77e0c0a04b2bb0d6f7a9e2c31845";

    [Fact]
    public void ToDetailResponse_MapsEventFieldsAndEmptyPlatforms()
    {
        var detail = new CalendarEventDetailView(CreateEvent(), []);

        var response = CalendarEventsApi.ToDetailResponse(detail);

        Assert.Equal(CalendarEventId, response.CalendarEventId);
        Assert.Equal(new DateTime(2026, 6, 15, 10, 0, 0), response.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", response.Start.TimeZoneId);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            response.ScheduledStartUtc);

        var description = Assert.Single(response.Descriptions);
        Assert.Equal("en", description.Language);
        Assert.Equal("English stream 1", description.Title);
        Assert.Null(description.Description);

        Assert.Empty(response.Platforms);
    }

    [Fact]
    public void ToDetailResponse_MapsPlatformItems()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var deletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);
        var views = new[]
        {
            new EventPlatformView(
                PlatformId,
                "Main YouTube channel",
                PlatformType.YouTube,
                PublishStatus.NotPublished,
                null,
                null,
                null,
                CanPublish: true),
            new EventPlatformView(
                OrphanPlatformId,
                "Old channel",
                PlatformType.YouTube,
                PublishStatus.Published,
                "oldyoutubeid",
                publishedUtc,
                deletedUtc,
                CanPublish: false)
        };
        var detail = new CalendarEventDetailView(CreateEvent(), views);

        var response = CalendarEventsApi.ToDetailResponse(detail);

        Assert.Equal(2, response.Platforms.Count);

        var active = response.Platforms[0];
        Assert.Equal(PlatformId, active.PlatformId);
        Assert.Equal("Main YouTube channel", active.PlatformName);
        Assert.Equal("YouTube", active.PlatformType);
        Assert.Equal("NotPublished", active.Status);
        Assert.Null(active.ExternalResourceId);
        Assert.Null(active.PublishedUtc);
        Assert.Null(active.PlatformDeletedUtc);
        Assert.True(active.CanPublish);

        var orphan = response.Platforms[1];
        Assert.Equal(OrphanPlatformId, orphan.PlatformId);
        Assert.Equal("Published", orphan.Status);
        Assert.Equal("oldyoutubeid", orphan.ExternalResourceId);
        Assert.Equal(publishedUtc, orphan.PublishedUtc);
        Assert.Equal(deletedUtc, orphan.PlatformDeletedUtc);
        Assert.False(orphan.CanPublish);
    }

    [Fact]
    public void ToEventPlatformResponse_PublishedView_MapsResourceAndTimestamps()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var view = new EventPlatformView(
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            PublishStatus.Published,
            "abc123youtubeid",
            publishedUtc,
            null,
            CanPublish: false);

        var response = CalendarEventsApi.ToEventPlatformResponse(view);

        Assert.Equal("Published", response.Status);
        Assert.Equal("abc123youtubeid", response.ExternalResourceId);
        Assert.Equal(publishedUtc, response.PublishedUtc);
        Assert.Null(response.PlatformDeletedUtc);
        Assert.False(response.CanPublish);
    }

    [Fact]
    public void ToEventPlatformResponse_OrphanView_SetsPlatformDeletedUtcAndCannotPublish()
    {
        var deletedUtc = new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);
        var view = new EventPlatformView(
            PlatformId,
            "Old channel",
            PlatformType.YouTube,
            PublishStatus.Published,
            "oldyoutubeid",
            new DateTimeOffset(2026, 6, 20, 8, 0, 0, TimeSpan.Zero),
            deletedUtc,
            CanPublish: false);

        var response = CalendarEventsApi.ToEventPlatformResponse(view);

        Assert.Equal(deletedUtc, response.PlatformDeletedUtc);
        Assert.False(response.CanPublish);
    }

    private static CalendarEventView CreateEvent() =>
        new(
            CalendarEventId,
            new ScheduledStart(new DateTime(2026, 6, 15, 10, 0, 0), "America/Vancouver"),
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            [new LocalizedDescription("en", "English stream 1", null)]);
}
