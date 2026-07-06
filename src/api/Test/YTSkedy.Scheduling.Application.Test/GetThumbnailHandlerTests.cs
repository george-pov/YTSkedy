using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class GetThumbnailHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string BlobName = $"calendar-events/{CalendarEventId}/thumbnail";

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var store = new FakeThumbnailStore(null);
        var handler = new GetThumbnailHandler(
            new FakeCalendarEventReader(null),
            new FakeThumbnailReader(null),
            store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.EventNotFound, result.Status);
        Assert.Equal(0, store.GetCallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingMetadata_ReturnsThumbnailNotFound()
    {
        var store = new FakeThumbnailStore(null);
        var handler = new GetThumbnailHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakeThumbnailReader(null),
            store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.ThumbnailNotFound, result.Status);
        Assert.Equal(0, store.GetCallCount);
    }

    [Fact]
    public async Task HandleAsync_MissingBlob_ReturnsThumbnailNotFound()
    {
        var store = new FakeThumbnailStore(null);
        var handler = new GetThumbnailHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakeThumbnailReader(CreateThumbnail()),
            store);

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.ThumbnailNotFound, result.Status);
        Assert.Equal(1, store.GetCallCount);
        Assert.Equal(BlobName, store.ReadBlobName);
    }

    [Fact]
    public async Task HandleAsync_ExistingBlob_ReturnsContent()
    {
        var content = new ThumbnailContent([1, 2, 3], "image/png");
        var handler = new GetThumbnailHandler(
            new FakeCalendarEventReader(CreateEvent()),
            new FakeThumbnailReader(CreateThumbnail()),
            new FakeThumbnailStore(content));

        var result = await handler.HandleAsync(CalendarEventId, CancellationToken.None);

        Assert.Equal(GetThumbnailStatus.Found, result.Status);
        Assert.Same(content, result.Content);
    }

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
        new(
            "stream.png",
            "image/png",
            123,
            1280,
            720,
            new DateTimeOffset(2026, 7, 6, 12, 0, 0, TimeSpan.Zero),
            BlobName);

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

    private sealed class FakeThumbnailReader(Thumbnail? thumbnail) : ICalendarEventThumbnailReader
    {
        public Task<Thumbnail?> GetThumbnailAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(thumbnail);
    }

    private sealed class FakeThumbnailStore(ThumbnailContent? content) : IThumbnailStore
    {
        public int GetCallCount { get; private set; }

        public string? ReadBlobName { get; private set; }

        public Task SaveAsync(
            string blobName,
            byte[] content,
            string contentType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ThumbnailContent?> GetAsync(
            string blobName,
            CancellationToken cancellationToken)
        {
            GetCallCount++;
            ReadBlobName = blobName;

            return Task.FromResult(content);
        }

        public Task DeleteAsync(
            string blobName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
