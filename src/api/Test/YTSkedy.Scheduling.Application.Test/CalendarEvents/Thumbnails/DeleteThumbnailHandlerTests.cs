using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class DeleteThumbnailHandlerTests
{
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<IPlatformPublicationReader> _publications = new();
    private readonly Mock<ICalendarEventThumbnailReader> _thumbnails = new();
    private readonly Mock<ICalendarEventThumbnailModifier> _modifier = new();
    private readonly Mock<IThumbnailStore> _store = new();
    private readonly DeleteThumbnailHandler _handler;

    public DeleteThumbnailHandlerTests()
    {
        _handler = new DeleteThumbnailHandler(
            _calendarEvents.Object,
            new CalendarEventPublicationLock(_publications.Object),
            _thumbnails.Object,
            _modifier.Object,
            _store.Object);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var handler = CreateHandler(null, null);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.EventNotFound, result.Status);
        _modifier.Verify(candidate => candidate.DeleteThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_PublicationRowsExist_ReturnsConflictWithoutDeleting()
    {
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            ApplicationTestData.Thumbnail(),
            canMutate: false);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.HasPlatformPublications, result.Status);
        _modifier.Verify(candidate => candidate.DeleteThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_MissingThumbnail_ReturnsThumbnailNotFound()
    {
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            null);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.ThumbnailNotFound, result.Status);
        _modifier.Verify(candidate => candidate.DeleteThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_ExistingThumbnail_ClearsMetadataAndDeletesBlob()
    {
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            ApplicationTestData.Thumbnail());

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.Deleted, result.Status);
        _modifier.Verify(candidate => candidate.DeleteThumbnailAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None));
        _store.Verify(candidate => candidate.DeleteAsync(
            ApplicationTestData.ThumbnailBlobName(),
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_MetadataConflict_ReturnsConflictWithoutDeletingBlob()
    {
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            ApplicationTestData.Thumbnail());
        _modifier
            .Setup(candidate => candidate.DeleteThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .Returns(Task.FromResult(CalendarEventChangeResult.Conflict));

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.Conflict, result.Status);
        _store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    private DeleteThumbnailHandler CreateHandler(
        CalendarEventView? calendarEvent,
        Thumbnail? thumbnail,
        bool canMutate = true)
    {
        _calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        _publications
            .Setup(candidate => candidate.HasAnyForEventAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(!canMutate);
        _thumbnails
            .Setup(candidate => candidate.GetThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(thumbnail);
        _modifier
            .Setup(candidate => candidate.DeleteThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .Returns(Task.FromResult(CalendarEventChangeResult.Applied));
        _store
            .Setup(candidate => candidate.DeleteAsync(
                ApplicationTestData.ThumbnailBlobName(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        return _handler;
    }
}
