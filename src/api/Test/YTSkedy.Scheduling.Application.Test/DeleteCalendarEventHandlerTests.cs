using YTSkedy.Scheduling.Application.CalendarEvents;
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
        var modifier = new FakeCalendarEventModifier();
        var handler = CreateHandler(calendarEvent: null, modifier);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.NotFound, result);
        Assert.Equal(0, modifier.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_ExistingEvent_DeletesRowAndReturnsDeleted()
    {
        var modifier = new FakeCalendarEventModifier();
        var handler = CreateHandler(CreateCalendarEventView(), modifier);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, modifier.DeleteCallCount);
        Assert.Equal(CalendarEventId, modifier.DeletedCalendarEventId);
    }

    [Fact]
    public async Task Delete_EventWithPlatformPublications_ReturnsConflictWithoutDeleting()
    {
        var modifier = new FakeCalendarEventModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(
            CreateCalendarEventView(),
            modifier,
            thumbnail: CreateThumbnail(),
            store: store,
            canMutate: false);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.HasPlatformPublications, result);
        Assert.Equal(0, modifier.DeleteCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_ExistingEventWithThumbnail_DeletesBlob()
    {
        var modifier = new FakeCalendarEventModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(
            CreateCalendarEventView(),
            modifier,
            thumbnail: CreateThumbnail(),
            store: store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, modifier.DeleteCallCount);
        Assert.Equal(1, store.DeleteCallCount);
        Assert.Equal(BlobName, store.DeletedBlobName);
    }

    [Fact]
    public async Task Delete_ThumbnailCleanupFails_ReturnsDeleted()
    {
        var modifier = new FakeCalendarEventModifier();
        var store = new FakeThumbnailStore { ThrowOnDelete = true };
        var handler = CreateHandler(
            CreateCalendarEventView(),
            modifier,
            thumbnail: CreateThumbnail(),
            store: store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteCalendarEventResult.Deleted, result);
        Assert.Equal(1, modifier.DeleteCallCount);
        Assert.Equal(1, store.DeleteCallCount);
    }

    [Fact]
    public async Task Delete_BlankId_Throws()
    {
        var handler = CreateHandler(CreateCalendarEventView(), new FakeCalendarEventModifier());

        await Assert.ThrowsAsync<ArgumentException>(
            () => handler.HandleAsync("   ", CancellationToken.None));
    }

    private static DeleteCalendarEventHandler CreateHandler(
        CalendarEventView? calendarEvent,
        FakeCalendarEventModifier modifier,
        Thumbnail? thumbnail = null,
        FakeThumbnailStore? store = null,
        bool canMutate = true) =>
        new(
            new FakeCalendarEventReader(getResult: calendarEvent),
            new CalendarEventPublicationLock(
                new FakePlatformPublicationReader(
                    canMutate
                        ? []
                        : [ApplicationTestData.Publication(
                            PublishStatus.Published,
                            calendarEventId: CalendarEventId)])),
            modifier,
            new FakeThumbnailReader(thumbnail),
            store ?? new FakeThumbnailStore());

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

    private sealed class FakeCalendarEventModifier : ICalendarEventModifier
    {
        public int DeleteCallCount { get; private set; }

        public string? DeletedCalendarEventId { get; private set; }

        public Task<string> CreateAsync(
            CalendarEvent calendarEvent,
            DateTimeOffset scheduledStartUtc,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> UpdateAsync(
            string calendarEventId,
            CalendarEvent calendarEvent,
            DateTimeOffset scheduledStartUtc,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            DeleteCallCount++;
            DeletedCalendarEventId = calendarEventId;

            return Task.CompletedTask;
        }
    }

}
