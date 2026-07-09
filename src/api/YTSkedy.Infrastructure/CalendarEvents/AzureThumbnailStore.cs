using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;

namespace YTSkedy.Infrastructure.CalendarEvents;

public sealed class AzureThumbnailStore : IThumbnailStore
{
    private readonly IThumbnailBlobContainer _container;

    public AzureThumbnailStore(BlobContainerClient containerClient)
        : this(new AzureThumbnailBlobContainer(containerClient))
    {
    }

    internal AzureThumbnailStore(IThumbnailBlobContainer container)
    {
        _container = container;
    }

    public async Task SaveAsync(
        string blobName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        await _container.CreateIfNotExistsAsync(cancellationToken);
        await _container.SaveAsync(blobName, content, contentType, cancellationToken);
    }

    public Task<ThumbnailContent?> GetAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        return _container.GetAsync(blobName, cancellationToken);
    }

    public Task DeleteAsync(
        string blobName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        return _container.DeleteIfExistsAsync(blobName, cancellationToken);
    }

    private sealed class AzureThumbnailBlobContainer(BlobContainerClient containerClient) :
        IThumbnailBlobContainer
    {
        public async Task CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            await containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None,
                cancellationToken: cancellationToken);
        }

        public async Task SaveAsync(
            string blobName,
            byte[] content,
            string contentType,
            CancellationToken cancellationToken)
        {
            var blobClient = containerClient.GetBlobClient(blobName);
            using var stream = new MemoryStream(content, writable: false);

            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = contentType
                    }
                },
                cancellationToken);
        }

        public async Task<ThumbnailContent?> GetAsync(
            string blobName,
            CancellationToken cancellationToken)
        {
            var blobClient = containerClient.GetBlobClient(blobName);

            try
            {
                var response = await blobClient.DownloadContentAsync(cancellationToken);
                var contentType = response.Value.Details.ContentType;

                return new ThumbnailContent(
                    response.Value.Content.ToArray(),
                    string.IsNullOrWhiteSpace(contentType)
                        ? "application/octet-stream"
                        : contentType);
            }
            catch (RequestFailedException exception) when (exception.Status == 404)
            {
                return null;
            }
        }

        public async Task DeleteIfExistsAsync(
            string blobName,
            CancellationToken cancellationToken)
        {
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.None,
                conditions: null,
                cancellationToken);
        }
    }
}

internal interface IThumbnailBlobContainer
{
    Task CreateIfNotExistsAsync(CancellationToken cancellationToken);

    Task SaveAsync(
        string blobName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken);

    Task<ThumbnailContent?> GetAsync(
        string blobName,
        CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(
        string blobName,
        CancellationToken cancellationToken);
}
