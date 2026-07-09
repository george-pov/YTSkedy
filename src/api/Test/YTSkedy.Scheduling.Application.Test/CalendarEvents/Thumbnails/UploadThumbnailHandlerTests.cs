using System.Buffers.Binary;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.TestSupport;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class UploadThumbnailHandlerTests
{
    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNotFoundWithoutSaving()
    {
        var store = new FakeThumbnailStore();
        var modifier = new FakeThumbnailModifier();
        var handler = CreateHandler(null, modifier, store);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal(UploadThumbnailStatus.EventNotFound, result.Status);
        Assert.Equal(0, store.SaveCallCount);
        Assert.Equal(0, modifier.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_PublicationRowsExist_ReturnsConflictWithoutSaving()
    {
        var store = new FakeThumbnailStore();
        var modifier = new FakeThumbnailModifier();
        var handler = CreateHandler(CreateEvent(), modifier, store, canMutate: false);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal(UploadThumbnailStatus.HasPlatformPublications, result.Status);
        Assert.Equal(0, store.SaveCallCount);
        Assert.Equal(0, modifier.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_InvalidUpload_ReturnsValidationErrorWithoutSaving()
    {
        var store = new FakeThumbnailStore();
        var modifier = new FakeThumbnailModifier();
        var handler = CreateHandler(CreateEvent(), modifier, store);
        var command = ValidCommand() with { FileName = "stream.gif" };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UploadThumbnailStatus.Invalid, result.Status);
        Assert.Equal(ThumbnailValidationError.UnsupportedExtension, result.ValidationError);
        Assert.Equal(0, store.SaveCallCount);
        Assert.Equal(0, modifier.SaveCallCount);
    }

    [Fact]
    public async Task HandleAsync_ValidUpload_SavesBlobThenMetadata()
    {
        var store = new FakeThumbnailStore();
        var modifier = new FakeThumbnailModifier();
        var handler = CreateHandler(CreateEvent(), modifier, store);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal(UploadThumbnailStatus.Uploaded, result.Status);
        Assert.NotNull(result.Thumbnail);
        Assert.Equal("stream.png", result.Thumbnail!.FileName);
        Assert.Equal("image/png", result.Thumbnail.ContentType);
        Assert.Equal(1280, result.Thumbnail.Width);
        Assert.Equal(720, result.Thumbnail.Height);
        Assert.Equal(ApplicationTestData.Now, result.Thumbnail.UpdatedUtc);
        Assert.Equal(
            ApplicationTestData.ThumbnailBlobName(),
            result.Thumbnail.BlobName);
        Assert.Equal(1, store.SaveCallCount);
        Assert.Equal(result.Thumbnail.BlobName, store.SavedBlobName);
        Assert.Equal(1, modifier.SaveCallCount);
        Assert.Same(result.Thumbnail, modifier.SavedThumbnail);
    }

    private static UploadThumbnailHandler CreateHandler(
        CalendarEventView? calendarEvent,
        FakeThumbnailModifier modifier,
        FakeThumbnailStore store,
        bool canMutate = true) =>
        new(
            new FakeCalendarEventReader(getResult: calendarEvent),
            new CalendarEventPublicationLock(
                new FakePlatformPublicationReader(
                    canMutate ? [] : [ApplicationTestData.Publication(PublishStatus.Published)])),
            modifier,
            store,
            new FixedTimeProvider(ApplicationTestData.Now));

    private static UploadThumbnailCommand ValidCommand() =>
        new(
            ApplicationTestData.CalendarEventId,
            @"C:\temp\stream.png",
            "image/png",
            Png(width: 1280, height: 720));

    private static CalendarEventView CreateEvent() =>
        ApplicationTestData.CalendarEvent(
            start: new ScheduledStart(new DateTime(2026, 7, 10, 10, 0, 0), "UTC"),
            scheduledStartUtc: new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero),
            text: ApplicationTestData.Text("Stream", "Description"));

    private static byte[] Png(int width, int height)
    {
        var content = new byte[24];
        byte[] signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(content, 0);
        "IHDR"u8.CopyTo(content.AsSpan(12, 4));
        BinaryPrimitives.WriteInt32BigEndian(content.AsSpan(16, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(content.AsSpan(20, 4), height);

        return content;
    }

    private sealed class FakeThumbnailModifier : ICalendarEventThumbnailModifier
    {
        public int SaveCallCount { get; private set; }

        public Thumbnail? SavedThumbnail { get; private set; }

        public Task<bool> SaveThumbnailAsync(
            string calendarEventId,
            Thumbnail thumbnail,
            CancellationToken cancellationToken)
        {
            SaveCallCount++;
            SavedThumbnail = thumbnail;

            return Task.FromResult(true);
        }

        public Task<bool> DeleteThumbnailAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
