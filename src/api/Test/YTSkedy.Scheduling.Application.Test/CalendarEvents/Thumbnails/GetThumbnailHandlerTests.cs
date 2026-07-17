using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class GetThumbnailHandlerTests
{
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<ICalendarEventThumbnailReader> _thumbnails = new();
    private readonly Mock<IThumbnailStore> _store = new();
    private readonly GetThumbnailHandler _handler;

    public GetThumbnailHandlerTests()
    {
        _handler = new GetThumbnailHandler(
            _calendarEvents.Object,
            _thumbnails.Object,
            _store.Object);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        CalendarEventReader(null);
        ThumbnailReader(null);

        var result = await _handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.EventNotFound, result.Status);
        _store.Verify(candidate => candidate.GetAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_MissingMetadata_ReturnsThumbnailNotFound()
    {
        CalendarEventReader(ApplicationTestData.CalendarEvent());
        ThumbnailReader(null);

        var result = await _handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.ThumbnailNotFound, result.Status);
        _store.Verify(candidate => candidate.GetAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_MissingBlob_ReturnsThumbnailNotFound()
    {
        CalendarEventReader(ApplicationTestData.CalendarEvent());
        ThumbnailReader(ApplicationTestData.Thumbnail());
        _store
            .Setup(candidate => candidate.GetAsync(
                ApplicationTestData.ThumbnailBlobName(),
                CancellationToken.None))
            .ReturnsAsync((ThumbnailContent?)null);
        var result = await _handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.ThumbnailNotFound, result.Status);
        _store.Verify(candidate => candidate.GetAsync(
            ApplicationTestData.ThumbnailBlobName(),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ExistingBlob_ReturnsContent()
    {
        var content = new ThumbnailContent([1, 2, 3], "image/png");
        CalendarEventReader(ApplicationTestData.CalendarEvent());
        ThumbnailReader(ApplicationTestData.Thumbnail());
        _store
            .Setup(candidate => candidate.GetAsync(
                ApplicationTestData.ThumbnailBlobName(),
                CancellationToken.None))
            .ReturnsAsync(content);
        var result = await _handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.Found, result.Status);
        Assert.Same(content, result.Content);
    }

    private Mock<ICalendarEventReader> CalendarEventReader(
        CalendarEventView? calendarEvent)
    {
        _calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        return _calendarEvents;
    }

    private Mock<ICalendarEventThumbnailReader> ThumbnailReader(
        Thumbnail? thumbnail)
    {
        _thumbnails
            .Setup(candidate => candidate.GetThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(thumbnail);
        return _thumbnails;
    }
}
