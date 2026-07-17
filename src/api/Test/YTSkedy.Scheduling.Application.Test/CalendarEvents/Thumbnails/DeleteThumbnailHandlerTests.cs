using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class DeleteThumbnailHandlerTests
{
    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var modifier = new Mock<ICalendarEventThumbnailModifier>();
        var store = new Mock<IThumbnailStore>();
        var handler = CreateHandler(null, null, modifier, store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.EventNotFound, result.Status);
        modifier.Verify(candidate => candidate.DeleteThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_PublicationRowsExist_ReturnsConflictWithoutDeleting()
    {
        var modifier = new Mock<ICalendarEventThumbnailModifier>();
        var store = new Mock<IThumbnailStore>();
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            ApplicationTestData.Thumbnail(),
            modifier,
            store,
            canMutate: false);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.HasPlatformPublications, result.Status);
        modifier.Verify(candidate => candidate.DeleteThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_MissingThumbnail_ReturnsThumbnailNotFound()
    {
        var modifier = new Mock<ICalendarEventThumbnailModifier>();
        var store = new Mock<IThumbnailStore>();
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            null,
            modifier,
            store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.ThumbnailNotFound, result.Status);
        modifier.Verify(candidate => candidate.DeleteThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_ExistingThumbnail_ClearsMetadataAndDeletesBlob()
    {
        var modifier = new Mock<ICalendarEventThumbnailModifier>();
        var store = new Mock<IThumbnailStore>();
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            ApplicationTestData.Thumbnail(),
            modifier,
            store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.Deleted, result.Status);
        modifier.Verify(candidate => candidate.DeleteThumbnailAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None));
        store.Verify(candidate => candidate.DeleteAsync(
            ApplicationTestData.ThumbnailBlobName(),
            CancellationToken.None));
    }

    private static DeleteThumbnailHandler CreateHandler(
        CalendarEventView? calendarEvent,
        Thumbnail? thumbnail,
        Mock<ICalendarEventThumbnailModifier> modifier,
        Mock<IThumbnailStore> store,
        bool canMutate = true)
    {
        var calendarEvents = new Mock<ICalendarEventReader>();
        calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        var publications = new Mock<IPlatformPublicationReader>();
        publications
            .Setup(candidate => candidate.HasAnyForEventAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(!canMutate);
        var thumbnails = new Mock<ICalendarEventThumbnailReader>();
        thumbnails
            .Setup(candidate => candidate.GetThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(thumbnail);
        modifier
            .Setup(candidate => candidate.DeleteThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(true);
        store
            .Setup(candidate => candidate.DeleteAsync(
                ApplicationTestData.ThumbnailBlobName(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        return new DeleteThumbnailHandler(
            calendarEvents.Object,
            new CalendarEventPublicationLock(publications.Object),
            thumbnails.Object,
            modifier.Object,
            store.Object);
    }
}
