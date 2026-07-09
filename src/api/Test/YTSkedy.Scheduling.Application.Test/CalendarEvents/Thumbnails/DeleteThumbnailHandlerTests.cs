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
        var modifier = new FakeThumbnailModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(null, null, modifier, store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.EventNotFound, result.Status);
        Assert.Equal(0, modifier.DeleteCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task HandleAsync_PublicationRowsExist_ReturnsConflictWithoutDeleting()
    {
        var modifier = new FakeThumbnailModifier();
        var store = new FakeThumbnailStore();
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
        Assert.Equal(0, modifier.DeleteCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingThumbnail_ReturnsThumbnailNotFound()
    {
        var modifier = new FakeThumbnailModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            null,
            modifier,
            store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.ThumbnailNotFound, result.Status);
        Assert.Equal(0, modifier.DeleteCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task HandleAsync_ExistingThumbnail_ClearsMetadataAndDeletesBlob()
    {
        var modifier = new FakeThumbnailModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(
            ApplicationTestData.CalendarEvent(),
            ApplicationTestData.Thumbnail(),
            modifier,
            store);

        var result = await handler.HandleAsync(
            ApplicationTestData.CalendarEventId,
            CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.Deleted, result.Status);
        Assert.Equal(1, modifier.DeleteCallCount);
        Assert.Equal(ApplicationTestData.CalendarEventId, modifier.DeletedCalendarEventId);
        Assert.Equal(1, store.DeleteCallCount);
        Assert.Equal(ApplicationTestData.ThumbnailBlobName(), store.DeletedBlobName);
    }

    private static DeleteThumbnailHandler CreateHandler(
        CalendarEventView? calendarEvent,
        Thumbnail? thumbnail,
        FakeThumbnailModifier modifier,
        FakeThumbnailStore store,
        bool canMutate = true) =>
        new(
            new FakeCalendarEventReader(getResult: calendarEvent),
            new CalendarEventPublicationLock(
                new FakePlatformPublicationReader(
                    canMutate ? [] : [ApplicationTestData.Publication(PublishStatus.Published)])),
            new FakeThumbnailReader(thumbnail),
            modifier,
            store);

    private sealed class FakeThumbnailModifier : ICalendarEventThumbnailModifier
    {
        public int DeleteCallCount { get; private set; }

        public string? DeletedCalendarEventId { get; private set; }

        public Task<bool> SaveThumbnailAsync(
            string calendarEventId,
            Thumbnail thumbnail,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteThumbnailAsync(
            string calendarEventId,
            CancellationToken cancellationToken)
        {
            DeleteCallCount++;
            DeletedCalendarEventId = calendarEventId;

            return Task.FromResult(true);
        }
    }
}
