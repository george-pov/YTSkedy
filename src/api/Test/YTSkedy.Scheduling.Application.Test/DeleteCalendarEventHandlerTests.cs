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
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<IPlatformPublicationReader> _publications = new();
    private readonly Mock<ICalendarEventThumbnailReader> _thumbnails = new();
    private readonly Mock<ICalendarEventModifier> _modifier = new();
    private readonly Mock<IThumbnailStore> _store = new();
    private readonly DeleteCalendarEventHandler _handler;

    public DeleteCalendarEventHandlerTests()
    {
        _handler = new DeleteCalendarEventHandler(
            _calendarEvents.Object,
            new CalendarEventPublicationLock(_publications.Object),
            _modifier.Object,
            _thumbnails.Object,
            _store.Object);
    }

    [Fact]
    public async Task Delete_MissingEvent_ReturnsNotFoundWithoutDeleting()
    {
        var handler = CreateHandler(calendarEvent: null);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotFound, result);
        _modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Delete_ExistingEvent_DeletesRowAndReturnsDeleted()
    {
        var handler = CreateHandler(CreateCalendarEventView());

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        _modifier.Verify(candidate => candidate.DeleteAsync(
            CalendarEventId,
            CancellationToken.None));
    }

    [Fact]
    public async Task Delete_EventWithPlatformPublications_ReturnsConflictWithoutDeleting()
    {
        var handler = CreateHandler(
            CreateCalendarEventView(),
            thumbnail: CreateThumbnail(),
            canMutate: false);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.HasPlatformPublications, result);
        _modifier.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _store.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task Delete_ExistingEventWithThumbnail_DeletesBlob()
    {
        _store
            .Setup(candidate => candidate.DeleteAsync(BlobName, CancellationToken.None))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(
            CreateCalendarEventView(),
            thumbnail: CreateThumbnail());

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        _modifier.Verify(candidate => candidate.DeleteAsync(
            CalendarEventId,
            CancellationToken.None));
        _store.Verify(candidate => candidate.DeleteAsync(BlobName, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_ThumbnailCleanupFails_ReturnsDeleted()
    {
        _store
            .Setup(candidate => candidate.DeleteAsync(BlobName, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Blob delete failed."));
        var handler = CreateHandler(
            CreateCalendarEventView(),
            thumbnail: CreateThumbnail());

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        _modifier.Verify(candidate => candidate.DeleteAsync(
            CalendarEventId,
            CancellationToken.None));
        _store.Verify(candidate => candidate.DeleteAsync(BlobName, CancellationToken.None));
    }

    [Fact]
    public async Task Delete_BlankId_Throws()
    {
        var handler = CreateHandler(CreateCalendarEventView());

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync("   ", CancellationToken.None));
    }

    private DeleteCalendarEventHandler CreateHandler(
        CalendarEventView? calendarEvent,
        Thumbnail? thumbnail = null,
        bool canMutate = true)
    {
        _calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        _publications
            .Setup(candidate => candidate.HasAnyForEventAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(!canMutate);
        _thumbnails
            .Setup(candidate => candidate.GetThumbnailAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(thumbnail);
        _modifier
            .Setup(candidate => candidate.DeleteAsync(
                CalendarEventId,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        return _handler;
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
