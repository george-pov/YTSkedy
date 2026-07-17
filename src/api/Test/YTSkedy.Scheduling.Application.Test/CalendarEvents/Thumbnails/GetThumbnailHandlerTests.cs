using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class GetThumbnailHandlerTests
{
    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var events = CalendarEventReader(null);
        var thumbnails = ThumbnailReader(null);
        var store = new Mock<IThumbnailStore>();
        var handler = new GetThumbnailHandler(
            events.Object,
            thumbnails.Object,
            store.Object);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.EventNotFound, result.Status);
        store.Verify(candidate => candidate.GetAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_MissingMetadata_ReturnsThumbnailNotFound()
    {
        var events = CalendarEventReader(ApplicationTestData.CalendarEvent());
        var thumbnails = ThumbnailReader(null);
        var store = new Mock<IThumbnailStore>();
        var handler = new GetThumbnailHandler(
            events.Object,
            thumbnails.Object,
            store.Object);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.ThumbnailNotFound, result.Status);
        store.Verify(candidate => candidate.GetAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_MissingBlob_ReturnsThumbnailNotFound()
    {
        var events = CalendarEventReader(ApplicationTestData.CalendarEvent());
        var thumbnails = ThumbnailReader(ApplicationTestData.Thumbnail());
        var store = new Mock<IThumbnailStore>();
        store
            .Setup(candidate => candidate.GetAsync(
                ApplicationTestData.ThumbnailBlobName(),
                CancellationToken.None))
            .ReturnsAsync((ThumbnailContent?)null);
        var handler = new GetThumbnailHandler(
            events.Object,
            thumbnails.Object,
            store.Object);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.ThumbnailNotFound, result.Status);
        store.Verify(candidate => candidate.GetAsync(
            ApplicationTestData.ThumbnailBlobName(),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ExistingBlob_ReturnsContent()
    {
        var content = new ThumbnailContent([1, 2, 3], "image/png");
        var events = CalendarEventReader(ApplicationTestData.CalendarEvent());
        var thumbnails = ThumbnailReader(ApplicationTestData.Thumbnail());
        var store = new Mock<IThumbnailStore>();
        store
            .Setup(candidate => candidate.GetAsync(
                ApplicationTestData.ThumbnailBlobName(),
                CancellationToken.None))
            .ReturnsAsync(content);
        var handler = new GetThumbnailHandler(
            events.Object,
            thumbnails.Object,
            store.Object);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.Found, result.Status);
        Assert.Same(content, result.Content);
    }

    private static Mock<ICalendarEventReader> CalendarEventReader(
        CalendarEventView? calendarEvent)
    {
        var reader = new Mock<ICalendarEventReader>();
        reader
            .Setup(candidate => candidate.GetByIdAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        return reader;
    }

    private static Mock<ICalendarEventThumbnailReader> ThumbnailReader(
        Thumbnail? thumbnail)
    {
        var reader = new Mock<ICalendarEventThumbnailReader>();
        reader
            .Setup(candidate => candidate.GetThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(thumbnail);
        return reader;
    }
}
