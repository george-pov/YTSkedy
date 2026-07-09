namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public interface IPublicationThumbnailWriter
{
    /// <summary>
    /// Records that the secondary thumbnail application succeeded after the
    /// provider resource was created and the row was marked
    /// <see cref="Domain.Platforms.PublishStatus.Published"/>. Returns false
    /// when the row no longer exists or no longer represents a completed
    /// publication.
    /// </summary>
    Task<bool> MarkThumbnailAppliedAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Records that the secondary thumbnail application failed after the
    /// provider resource was created and the row was marked
    /// <see cref="Domain.Platforms.PublishStatus.Published"/>. Returns false
    /// when the row no longer exists or no longer represents a completed
    /// publication.
    /// </summary>
    Task<bool> MarkThumbnailFailedAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken);
}
