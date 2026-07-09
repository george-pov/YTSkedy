using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class GetThumbnailHandlerTests
{
    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var store = new FakeThumbnailStore(null);
        var handler = new GetThumbnailHandler(
            new FakeCalendarEventReader(getResult: null),
            new FakeThumbnailReader(null),
            store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.EventNotFound, result.Status);
        Assert.Equal(0, store.GetCallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingMetadata_ReturnsThumbnailNotFound()
    {
        var store = new FakeThumbnailStore(null);
        var handler = new GetThumbnailHandler(
            new FakeCalendarEventReader(getResult: ApplicationTestData.CalendarEvent()),
            new FakeThumbnailReader(null),
            store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.ThumbnailNotFound, result.Status);
        Assert.Equal(0, store.GetCallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingBlob_ReturnsThumbnailNotFound()
    {
        var store = new FakeThumbnailStore(null);
        var handler = new GetThumbnailHandler(
            new FakeCalendarEventReader(getResult: ApplicationTestData.CalendarEvent()),
            new FakeThumbnailReader(ApplicationTestData.Thumbnail()),
            store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.ThumbnailNotFound, result.Status);
        Assert.Equal(1, store.GetCallCount);
        Assert.Equal(ApplicationTestData.ThumbnailBlobName(), store.ReadBlobName);
    }

    [Fact]
    public async Task HandleAsync_ExistingBlob_ReturnsContent()
    {
        var content = new ThumbnailContent([1, 2, 3], "image/png");
        var handler = new GetThumbnailHandler(
            new FakeCalendarEventReader(getResult: ApplicationTestData.CalendarEvent()),
            new FakeThumbnailReader(ApplicationTestData.Thumbnail()),
            new FakeThumbnailStore(content));

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.Found, result.Status);
        Assert.Same(content, result.Content);
    }
}
