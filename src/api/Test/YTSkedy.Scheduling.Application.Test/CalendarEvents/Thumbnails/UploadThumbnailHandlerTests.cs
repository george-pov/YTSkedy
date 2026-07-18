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
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<IPlatformPublicationReader> _publications = new();
    private readonly Mock<ICalendarEventThumbnailModifier> _modifier = new();
    private readonly Mock<IThumbnailStore> _store = new();
    private readonly UploadThumbnailHandler _handler;

    public UploadThumbnailHandlerTests()
    {
        _handler = new UploadThumbnailHandler(
            _calendarEvents.Object,
            new CalendarEventPublicationLock(_publications.Object),
            _modifier.Object,
            _store.Object,
            new FixedTimeProvider(ApplicationTestData.Now));
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsNotFoundWithoutSaving()
    {
        var handler = CreateHandler(null);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal(UploadThumbnailStatus.EventNotFound, result.Status);
        _store.Verify(candidate => candidate.SaveAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _modifier.Verify(candidate => candidate.SaveThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<Thumbnail>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_PublicationRowsExist_ReturnsConflictWithoutSaving()
    {
        var handler = CreateHandler(CreateEvent(), canMutate: false);

        var result = await handler.HandleAsync(ValidCommand(), CancellationToken.None);

        Assert.Equal(UploadThumbnailStatus.HasPlatformPublications, result.Status);
        _store.Verify(candidate => candidate.SaveAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _modifier.Verify(candidate => candidate.SaveThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<Thumbnail>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_InvalidUpload_ReturnsValidationErrorWithoutSaving()
    {
        var handler = CreateHandler(CreateEvent());
        var command = ValidCommand() with { FileName = "stream.gif" };

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(UploadThumbnailStatus.Invalid, result.Status);
        Assert.Equal(ThumbnailValidationError.UnsupportedExtension, result.ValidationError);
        _store.Verify(candidate => candidate.SaveAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
        _modifier.Verify(candidate => candidate.SaveThumbnailAsync(
            It.IsAny<string>(),
            It.IsAny<Thumbnail>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_ValidUpload_SavesBlobThenMetadata()
    {
        var handler = CreateHandler(CreateEvent());

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
        _store.Verify(candidate => candidate.SaveAsync(
            result.Thumbnail.BlobName,
            It.Is<byte[]>(content => content.SequenceEqual(Png(1280, 720))),
            "image/png",
            CancellationToken.None));
        _modifier.Verify(candidate => candidate.SaveThumbnailAsync(
            ApplicationTestData.CalendarEventId,
            result.Thumbnail,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_MetadataConflict_ReturnsConflictAfterSavingBlob()
    {
        var handler = CreateHandler(CreateEvent());
        _modifier
            .Setup(candidate => candidate.SaveThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                It.IsAny<Thumbnail>(),
                CancellationToken.None))
            .Returns(Task.FromResult(CalendarEventChangeResult.Conflict));

        var result = await handler.HandleAsync(
            ValidCommand(),
            CancellationToken.None);

        Assert.Equal(UploadThumbnailStatus.Conflict, result.Status);
        _store.Verify(candidate => candidate.SaveAsync(
            ApplicationTestData.ThumbnailBlobName(),
            It.IsAny<byte[]>(),
            "image/png",
            CancellationToken.None));
    }

    private UploadThumbnailHandler CreateHandler(
        CalendarEventView? calendarEvent,
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
        _modifier
            .Setup(candidate => candidate.SaveThumbnailAsync(
                ApplicationTestData.CalendarEventId,
                It.IsAny<Thumbnail>(),
                CancellationToken.None))
            .Returns(Task.FromResult(CalendarEventChangeResult.Applied));
        _store
            .Setup(candidate => candidate.SaveAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        return _handler;
    }

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

}
