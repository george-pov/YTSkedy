using System.Buffers.Binary;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class CalendarEventThumbnailsApiTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private static readonly DateTimeOffset Now =
        new(2026, 7, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task UploadAsync_ThumbnailFile_ReturnsMetadata()
    {
        var api = new CalendarEventThumbnailsApi(
            new UploadThumbnailHandler(
                new FakeCalendarEventReader(CreateEvent()),
                new FakePlatformPublicationReader([]),
                new FakeThumbnailModifier(),
                new FakeThumbnailStore(),
                new FixedTimeProvider(Now)),
            null!,
            null!);
        var request = RequestWithFile(
            "thumbnail",
            "stream.png",
            "image/png",
            Png(width: 1280, height: 720));

        var result = await api.UploadAsync(
            request,
            CalendarEventId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<ThumbnailResponse>(ok.Value);
        Assert.Equal("stream.png", response.FileName);
        Assert.Equal("image/png", response.ContentType);
        Assert.Equal(1280, response.Width);
        Assert.Equal(720, response.Height);
        Assert.Equal(Now, response.UpdatedUtc);
    }

    [Fact]
    public async Task UploadAsync_MissingThumbnailPart_ReturnsBadRequest()
    {
        var api = new CalendarEventThumbnailsApi(null!, null!, null!);

        var result = await api.UploadAsync(
            RequestWithForm([]),
            CalendarEventId,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(
            "Multipart form-data file part 'thumbnail' is required.",
            badRequest.Value);
    }

    [Fact]
    public void ToUploadResult_Locked_ReturnsConflict()
    {
        var result = CalendarEventThumbnailsApi.ToUploadResult(
            UploadThumbnailResult.HasPlatformPublications,
            CalendarEventId);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public void ToUploadResult_Invalid_ReturnsBadRequest()
    {
        var result = CalendarEventThumbnailsApi.ToUploadResult(
            UploadThumbnailResult.Invalid(ThumbnailValidationError.TooLarge),
            CalendarEventId);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Thumbnail file size must be 2 MB or smaller.", badRequest.Value);
    }

    [Fact]
    public void ToGetResult_Found_ReturnsFileContent()
    {
        var result = CalendarEventThumbnailsApi.ToGetResult(
            GetThumbnailResult.Found(new ThumbnailContent([1, 2, 3], "image/png")),
            CalendarEventId);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.Equal([1, 2, 3], file.FileContents);
    }

    [Theory]
    [InlineData(GetThumbnailStatus.EventNotFound)]
    [InlineData(GetThumbnailStatus.ThumbnailNotFound)]
    public void ToGetResult_Missing_ReturnsNotFound(GetThumbnailStatus status)
    {
        var result = CalendarEventThumbnailsApi.ToGetResult(
            new GetThumbnailResult(status),
            CalendarEventId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void ToDeleteResult_Deleted_ReturnsNoContent()
    {
        var result = CalendarEventThumbnailsApi.ToDeleteResult(
            DeleteThumbnailResult.Deleted,
            CalendarEventId);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void ToDeleteResult_Locked_ReturnsConflict()
    {
        var result = CalendarEventThumbnailsApi.ToDeleteResult(
            DeleteThumbnailResult.HasPlatformPublications,
            CalendarEventId);

        Assert.IsType<ConflictObjectResult>(result);
    }

    private static HttpRequest RequestWithFile(
        string formName,
        string fileName,
        string contentType,
        byte[] content)
    {
        var file = new FormFile(
            new MemoryStream(content),
            0,
            content.Length,
            formName,
            fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };

        return RequestWithForm([file]);
    }

    private static HttpRequest RequestWithForm(IReadOnlyList<IFormFile> files)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "multipart/form-data; boundary=test";

        var formFiles = new FormFileCollection();
        foreach (var file in files)
        {
            formFiles.Add(file);
        }

        context.Features.Set<IFormFeature>(new FormFeature(new FormCollection(
            new Dictionary<string, StringValues>(),
            formFiles)));

        return context.Request;
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

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

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

    private sealed class FakeThumbnailModifier : ICalendarEventThumbnailModifier
    {
        public Task<bool> SaveThumbnailAsync(
            string calendarEventId,
            Thumbnail thumbnail,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> DeleteThumbnailAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeThumbnailStore : IThumbnailStore
    {
        public Task SaveAsync(
            string blobName,
            byte[] content,
            string contentType,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<ThumbnailContent?> GetAsync(
            string blobName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string blobName,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
