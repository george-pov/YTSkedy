using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class GetCalendarEventDetailsResponseTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;
    private const string OrphanPlatformId = SchedulingSampleIds.OtherPlatformId;

    [Fact]
    public void ToDetailsResponse_MapsEventFieldsAndEmptyPlatforms()
    {
        var details = new CalendarEventDetailsView(
            CreateEvent(),
            CanUpdate: true,
            CanDelete: true,
            Platforms: []);

        var response = CalendarEventsApi.ToDetailsResponse(details);

        Assert.Equal(CalendarEventId, response.CalendarEventId);
        Assert.Equal(new DateTime(2026, 6, 15, 10, 0, 0), response.Start.LocalDateTime);
        Assert.Equal("America/Vancouver", response.Start.TimeZoneId);
        Assert.Equal(
            new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            response.ScheduledStartUtc);
        Assert.Equal("English stream 1", response.DisplayTitle);
        Assert.True(response.CanUpdate);
        Assert.True(response.CanDelete);
        Assert.Null(response.Thumbnail);
        Assert.True(response.CanUpdateThumbnail);

        Assert.Collection(
            response.Texts,
            first =>
            {
                Assert.Equal("text1", first.FieldKey);
                Assert.Equal("Title", first.Label);
                Assert.Equal("ShortText", first.Type);
                Assert.Equal(50, first.MaxLength);
                Assert.Equal("English stream 1", first.Value);
            },
            second =>
            {
                Assert.Equal("text2", second.FieldKey);
                Assert.Equal("Description", second.Label);
                Assert.Equal("LongText", second.Type);
                Assert.Equal(2500, second.MaxLength);
                Assert.Equal("Event description", second.Value);
            });

        Assert.Empty(response.Platforms);
    }

    [Fact]
    public void ToDetailsResponse_MapsPlatformItems()
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
                CanPublish: true,
                CanDeletePublication: false,
                CanPreviewPublishingContent: true,
                ThumbnailStatus: ThumbnailPublishStatus.NotConfigured),
            new EventPlatformView(
                OrphanPlatformId,
                "Old channel",
                PlatformType.YouTube,
                PublishStatus.Published,
                "oldyoutubeid",
                publishedUtc,
                deletedUtc,
                CanPublish: false,
                CanDeletePublication: false,
                CanPreviewPublishingContent: true,
                ThumbnailStatus: ThumbnailPublishStatus.Failed)
        };
        var details = new CalendarEventDetailsView(
            CreateEvent(),
            CanUpdate: false,
            CanDelete: false,
            Platforms: views,
            CanUpdateThumbnail: false);

        var response = CalendarEventsApi.ToDetailsResponse(details);

        Assert.False(response.CanUpdate);
        Assert.False(response.CanDelete);
        Assert.False(response.CanUpdateThumbnail);
        Assert.Equal(2, response.Platforms.Count);

        var active = response.Platforms[0];
        Assert.Equal(PlatformId, active.PlatformId);
        Assert.Equal("Main YouTube channel", active.PlatformName);
        Assert.Equal("YouTube", active.PlatformType);
        Assert.Equal("NotPublished", active.Status);
        Assert.Null(active.ExternalResourceId);
        Assert.Equal("NotConfigured", active.ThumbnailStatus);
        Assert.Null(active.PublishedUtc);
        Assert.Null(active.PlatformDeletedUtc);
        Assert.True(active.CanPublish);
        Assert.False(active.CanDeletePublication);
        Assert.True(active.CanPreviewPublishingContent);

        var orphan = response.Platforms[1];
        Assert.Equal(OrphanPlatformId, orphan.PlatformId);
        Assert.Equal("Published", orphan.Status);
        Assert.Equal("oldyoutubeid", orphan.ExternalResourceId);
        Assert.Equal("Failed", orphan.ThumbnailStatus);
        Assert.Equal(publishedUtc, orphan.PublishedUtc);
        Assert.Equal(deletedUtc, orphan.PlatformDeletedUtc);
        Assert.False(orphan.CanPublish);
        Assert.False(orphan.CanDeletePublication);
        Assert.True(orphan.CanPreviewPublishingContent);
    }

    [Fact]
    public void ToDetailsResponse_MapsThumbnailMetadataWithoutBlobName()
    {
        var updatedUtc = new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero);
        var details = new CalendarEventDetailsView(
            CreateEvent(),
            CanUpdate: true,
            CanDelete: true,
            Platforms: [],
            Thumbnail: new(
                "stream.png",
                "image/png",
                123,
                1280,
                720,
                updatedUtc,
                $"calendar-events/{CalendarEventId}/thumbnail"),
            CanUpdateThumbnail: true);

        var response = CalendarEventsApi.ToDetailsResponse(details);

        Assert.NotNull(response.Thumbnail);
        Assert.Equal("stream.png", response.Thumbnail!.FileName);
        Assert.Equal("image/png", response.Thumbnail.ContentType);
        Assert.Equal(123, response.Thumbnail.SizeBytes);
        Assert.Equal(1280, response.Thumbnail.Width);
        Assert.Equal(720, response.Thumbnail.Height);
        Assert.Equal(updatedUtc, response.Thumbnail.UpdatedUtc);
        Assert.True(response.CanUpdateThumbnail);
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
            CanPublish: false,
            CanDeletePublication: true,
            CanPreviewPublishingContent: true,
            ThumbnailStatus: ThumbnailPublishStatus.Applied);

        var response = CalendarEventsApi.ToEventPlatformResponse(view);

        Assert.Equal("Published", response.Status);
        Assert.Equal("abc123youtubeid", response.ExternalResourceId);
        Assert.Equal("Applied", response.ThumbnailStatus);
        Assert.Equal(publishedUtc, response.PublishedUtc);
        Assert.Null(response.PlatformDeletedUtc);
        Assert.False(response.CanPublish);
        Assert.True(response.CanDeletePublication);
        Assert.True(response.CanPreviewPublishingContent);
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
            CanPublish: false,
            CanDeletePublication: false,
            CanPreviewPublishingContent: true);

        var response = CalendarEventsApi.ToEventPlatformResponse(view);

        Assert.Equal(deletedUtc, response.PlatformDeletedUtc);
        Assert.False(response.CanPublish);
        Assert.False(response.CanDeletePublication);
        Assert.True(response.CanPreviewPublishingContent);
    }

    private static CalendarEventView CreateEvent() =>
        SchedulingSamples.CalendarEvent(
            calendarEventId: CalendarEventId,
            scheduledStartUtc: new DateTimeOffset(2026, 6, 15, 17, 0, 0, TimeSpan.Zero),
            start: SchedulingSamples.ScheduledStart(
                new DateTime(2026, 6, 15, 10, 0, 0),
                "America/Vancouver"),
            text: SchedulingSamples.Text(
                title: "English stream 1",
                description: "Event description"));
}
