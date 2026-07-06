using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class DeleteThumbnailHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string BlobName = $"calendar-events/{CalendarEventId}/thumbnail";
    private static readonly DateTimeOffset Now =
        new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var modifier = new FakeThumbnailModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(null, [], null, modifier, store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.EventNotFound, result.Status);
        Assert.Equal(0, modifier.DeleteCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task HandleAsync_PublicationRowsExist_ReturnsConflictWithoutDeleting()
    {
        var modifier = new FakeThumbnailModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(CreateEvent(), [Publication()], CreateThumbnail(), modifier, store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.HasPlatformPublications, result.Status);
        Assert.Equal(0, modifier.DeleteCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingThumbnail_ReturnsThumbnailNotFound()
    {
        var modifier = new FakeThumbnailModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(CreateEvent(), [], null, modifier, store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.ThumbnailNotFound, result.Status);
        Assert.Equal(0, modifier.DeleteCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task HandleAsync_ExistingThumbnail_ClearsMetadataAndDeletesBlob()
    {
        var modifier = new FakeThumbnailModifier();
        var store = new FakeThumbnailStore();
        var handler = CreateHandler(CreateEvent(), [], CreateThumbnail(), modifier, store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(DeleteThumbnailStatus.Deleted, result.Status);
        Assert.Equal(1, modifier.DeleteCallCount);
        Assert.Equal(CalendarEventId, modifier.DeletedCalendarEventId);
        Assert.Equal(1, store.DeleteCallCount);
        Assert.Equal(BlobName, store.DeletedBlobName);
    }

    private static DeleteThumbnailHandler CreateHandler(
        CalendarEventView? calendarEvent,
        IReadOnlyList<PlatformPublication> publications,
        Thumbnail? thumbnail,
        FakeThumbnailModifier modifier,
        FakeThumbnailStore store) =>
        new(
            new FakeCalendarEventReader(calendarEvent),
            new FakePlatformPublicationReader(publications),
            new FakeThumbnailReader(thumbnail),
            modifier,
            store);

    private static CalendarEventView CreateEvent() =>
        new(
            CalendarEventId,
            new ScheduledStart(new DateTime(2026, 7, 10, 10, 0, 0), "UTC"),
            new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero),
            EventTextSnapshot.Create(
                EventTextFields.Default,
                [
                    new EventTextValue("text1", "Stream"),
                    new EventTextValue("text2", "Description")
                ]));

    private static Thumbnail CreateThumbnail() =>
        new("stream.png", "image/png", 123, 1280, 720, Now, BlobName);

    private static PlatformPublication Publication() =>
        new(
            CalendarEventId,
            "platform-1",
            "Main YouTube channel",
            PlatformType.YouTube,
            PublishStatus.Published,
            "external-1",
            Now,
            null,
            Now);

    private sealed class FakeCalendarEventReader(CalendarEventView? calendarEvent) : ICalendarEventReader
    {
        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(calendarEvent);
    }

    private sealed class FakePlatformPublicationReader(IReadOnlyList<PlatformPublication> publications)
        : IPlatformPublicationReader
    {
        public Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(publications);

        public Task<PlatformPublication?> GetAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeThumbnailReader(Thumbnail? thumbnail) : ICalendarEventThumbnailReader
    {
        public Task<Thumbnail?> GetThumbnailAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(thumbnail);
    }

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

    private sealed class FakeThumbnailStore : IThumbnailStore
    {
        public int DeleteCallCount { get; private set; }

        public string? DeletedBlobName { get; private set; }

        public Task SaveAsync(
            string blobName,
            byte[] content,
            string contentType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ThumbnailContent?> GetAsync(
            string blobName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string blobName,
            CancellationToken cancellationToken)
        {
            DeleteCallCount++;
            DeletedBlobName = blobName;

            return Task.CompletedTask;
        }
    }
}
