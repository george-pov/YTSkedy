namespace YTSkedy.Scheduling.Application.CalendarEvents;

public interface IThumbnailStore
{
    Task SaveAsync(
        string blobName,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken);

    Task<ThumbnailContent?> GetAsync(
        string blobName,
        CancellationToken cancellationToken);

    Task DeleteAsync(
        string blobName,
        CancellationToken cancellationToken);
}
