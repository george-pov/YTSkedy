using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public interface IPlatformPublicationReader
{
    /// <summary>
    /// Reads every persisted publication row for one calendar event. Missing
    /// rows are not synthesized here; the caller treats an event/platform pair
    /// with no row as <see cref="PublishStatus.NotPublished"/>. Orphaned rows are
    /// included so the caller can render publish history for deleted platforms.
    /// The returned order is not significant.
    /// </summary>
    Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
        string calendarEventId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the publication row for one calendar event and one platform, or null
    /// when no row exists. A null result is the normal representation of
    /// <see cref="PublishStatus.NotPublished"/>.
    /// </summary>
    Task<PlatformPublication?> GetAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the publication rows for one platform that are still
    /// <see cref="PublishStatus.Publishing"/>, across all calendar events. The
    /// platform delete guard uses this to block deleting a platform while a
    /// publish is in progress.
    /// </summary>
    Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
        string platformId,
        CancellationToken cancellationToken);
}
