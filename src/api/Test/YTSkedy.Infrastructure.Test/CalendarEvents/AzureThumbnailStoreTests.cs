using YTSkedy.Infrastructure.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

namespace YTSkedy.Infrastructure.Test.CalendarEvents;

public sealed class AzureThumbnailStoreTests
{
    [Fact]
    public async Task SaveAsync_Content_SavesBlobWithoutProvisioningContainer()
    {
        var container = new FakeThumbnailBlobContainer();
        var store = new AzureThumbnailStore(container);
        byte[] content = [1, 2, 3];

        await store.SaveAsync(
            "calendar-events/event-1/thumbnail",
            content,
            "image/png",
            CancellationToken.None);

        Assert.Equal(1, container.SaveCallCount);
        Assert.Equal("calendar-events/event-1/thumbnail", container.SavedBlobName);
        Assert.Same(content, container.SavedContent);
        Assert.Equal("image/png", container.SavedContentType);
    }

    [Fact]
    public async Task GetAsync_ExistingBlob_ReturnsContent()
    {
        var content = new ThumbnailContent([1, 2, 3], "image/png");
        var store = new AzureThumbnailStore(new FakeThumbnailBlobContainer
        {
            Content = content
        });

        var result = await store.GetAsync(
            "calendar-events/event-1/thumbnail",
            CancellationToken.None);

        Assert.Same(content, result);
    }

    [Fact]
    public async Task DeleteAsync_BlobName_DeletesIfExists()
    {
        var container = new FakeThumbnailBlobContainer();
        var store = new AzureThumbnailStore(container);

        await store.DeleteAsync(
            "calendar-events/event-1/thumbnail",
            CancellationToken.None);

        Assert.Equal(1, container.DeleteCallCount);
        Assert.Equal("calendar-events/event-1/thumbnail", container.DeletedBlobName);
    }

    private sealed class FakeThumbnailBlobContainer : IThumbnailBlobContainer
    {
        public ThumbnailContent? Content { get; init; }

        public int SaveCallCount { get; private set; }

        public int DeleteCallCount { get; private set; }

        public string? SavedBlobName { get; private set; }

        public byte[]? SavedContent { get; private set; }

        public string? SavedContentType { get; private set; }

        public string? DeletedBlobName { get; private set; }

        public Task SaveAsync(
            string blobName,
            byte[] content,
            string contentType,
            CancellationToken cancellationToken)
        {
            SaveCallCount++;
            SavedBlobName = blobName;
            SavedContent = content;
            SavedContentType = contentType;

            return Task.CompletedTask;
        }

        public Task<ThumbnailContent?> GetAsync(
            string blobName,
            CancellationToken cancellationToken) =>
            Task.FromResult(Content);

        public Task DeleteIfExistsAsync(
            string blobName,
            CancellationToken cancellationToken)
        {
            DeleteCallCount++;
            DeletedBlobName = blobName;

            return Task.CompletedTask;
        }
    }
}
