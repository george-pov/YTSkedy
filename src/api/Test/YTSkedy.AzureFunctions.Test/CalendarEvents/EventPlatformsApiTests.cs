using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class EventPlatformsApiTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

    [Fact]
    public void ToListResponse_EchoesCalendarEventIdAndMapsItems()
    {
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
                CanPublish: true)
        };

        var response = EventPlatformsApi.ToListResponse(CalendarEventId, views);

        Assert.Equal(CalendarEventId, response.CalendarEventId);
        var item = Assert.Single(response.Items);
        Assert.Equal(PlatformId, item.PlatformId);
        Assert.Equal("Main YouTube channel", item.PlatformName);
        Assert.Equal("YouTube", item.PlatformType);
        Assert.Equal("NotPublished", item.Status);
        Assert.Null(item.ExternalResourceId);
        Assert.Null(item.PublishedUtc);
        Assert.Null(item.PlatformDeletedUtc);
        Assert.True(item.CanPublish);
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

        var response = EventPlatformsApi.ToEventPlatformResponse(view);

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

        var response = EventPlatformsApi.ToEventPlatformResponse(view);

        Assert.Equal(deletedUtc, response.PlatformDeletedUtc);
        Assert.False(response.CanPublish);
    }
}
