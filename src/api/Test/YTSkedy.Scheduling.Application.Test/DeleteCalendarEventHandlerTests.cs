using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class DeleteCalendarEventHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string BlobName = $"calendar-events/{CalendarEventId}/thumbnail";
    private static readonly DateTimeOffset StartUtc =
        new(2026, 06, 15, 17, 00, 00, TimeSpan.Zero);

    [Fact]
    public async Task Delete_MissingEvent_ReturnsNotFoundWithoutDeleting()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var handler = CreateHandler(calendarEvent: null, modifier);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotFound, result);
        modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Delete_ExistingEvent_DeletesRowAndReturnsDeleted()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var handler = CreateHandler(CreateCalendarEventView(), modifier);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        modifier.Verify(candidate => candidate.DeleteAsync(
            CalendarEventId,
            CancellationToken.None));
    }

    [Fact]
    public async Task Delete_EventWithPlatformPublications_ReturnsConflictWithoutDeleting()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var store = new Mock<IThumbnailStore>();
        var handler = CreateHandler(
            CreateCalendarEventView(),
            modifier,
            thumbnail: CreateThumbnail(),
            store: store,
            canMutate: false);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.HasPlatformPublications, result);
        modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Delete_ExistingEventWithThumbnail_DeletesBlob()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var store = new Mock<IThumbnailStore>();
        store
            .Setup(candidate => candidate.DeleteAsync(BlobName, CancellationToken.None))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(
            CreateCalendarEventView(),
            modifier,
            thumbnail: CreateThumbnail(),
            store: store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        modifier.Verify(candidate => candidate.DeleteAsync(
            CalendarEventId,
            CancellationToken.None));
        store.Verify(candidate => candidate.DeleteAsync(BlobName, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_ThumbnailCleanupFails_ReturnsDeleted()
    {
        var modifier = new Mock<ICalendarEventModifier>();
        var store = new Mock<IThumbnailStore>();
        store
            .Setup(candidate => candidate.DeleteAsync(BlobName, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Blob delete failed."));
        var handler = CreateHandler(
            CreateCalendarEventView(),
            modifier,
            thumbnail: CreateThumbnail(),
            store: store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        modifier.Verify(candidate => candidate.DeleteAsync(
            CalendarEventId,
            CancellationToken.None));
        store.Verify(candidate => candidate.DeleteAsync(BlobName, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_BlankId_Throws()
    {
        var handler = CreateHandler(
            CreateCalendarEventView(),
            new Mock<ICalendarEventModifier>());

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync("   ", CancellationToken.None));
    }

    private static DeleteCalendarEventHandler CreateHandler(
        CalendarEventView? calendarEvent,
        Mock<ICalendarEventModifier> modifier,
        Thumbnail? thumbnail = null,
        Mock<IThumbnailStore>? store = null,
        bool canMutate = true)
    {
        var calendarEvents = new Mock<ICalendarEventReader>();
        calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        var publications = new Mock<IPlatformPublicationReader>();
        publications
            .Setup(candidate => candidate.HasAnyForEventAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(!canMutate);
        var thumbnails = new Mock<ICalendarEventThumbnailReader>();
        thumbnails
            .Setup(candidate => candidate.GetThumbnailAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(thumbnail);
        modifier
            .Setup(candidate => candidate.DeleteAsync(
                CalendarEventId,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        return new DeleteCalendarEventHandler(
            calendarEvents.Object,
            new CalendarEventPublicationLock(publications.Object),
            modifier.Object,
            thumbnails.Object,
            (store ?? new Mock<IThumbnailStore>()).Object);
    }

    private static CalendarEventView CreateCalendarEventView() =>
        new(
            CalendarEventId,
            new ScheduledStart(StartUtc.UtcDateTime, "UTC"),
            StartUtc,
            EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "English title"),
                    new EventTextValue("text2", "English description")
                ]));

    private static Thumbnail CreateThumbnail() =>
        ApplicationTestData.Thumbnail(
            calendarEventId: CalendarEventId,
            updatedUtc: StartUtc,
            blobName: BlobName);

}
