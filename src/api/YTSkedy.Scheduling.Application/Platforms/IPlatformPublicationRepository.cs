namespace YTSkedy.Scheduling.Application.Platforms;

public interface IPlatformPublicationRepository
{
    /// <summary>
    /// Reserves a publication row by creating it directly as
    /// <see cref="Domain.Platforms.PublishStatus.Publishing"/> with the platform
    /// name, type, and publish settings copied from the reservation. The write is
    /// conditional on the row not already existing, so a concurrent reserve for
    /// the same event/platform pair yields
    /// <see cref="ReservePublicationResult.Conflict"/> and only one caller may
    /// proceed to the provider. Any existing row (publishing, published, or
    /// orphaned) is also a conflict in this iteration.
    /// </summary>
    Task<ReservePublicationResult> ReserveAsync(
        PlatformPublicationReservation reservation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases a reservation by removing the <c>Publishing</c> row, returning the
    /// event/platform pair to the computed
    /// <see cref="Domain.Platforms.PublishStatus.NotPublished"/> state. Used to
    /// roll back after a provider call fails before the row is marked published. A
    /// missing row is treated as already released.
    /// </summary>
    Task ReleaseAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks a reserved publication row
    /// <see cref="Domain.Platforms.PublishStatus.Published"/> after the provider
    /// call succeeds, recording the provider <paramref name="externalResourceId"/>
    /// and the publish instant. Returns the recorded publish instant, or null when
    /// no row exists for the pair.
    /// </summary>
    Task<DateTimeOffset?> MarkPublishedAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Orphans the <see cref="Domain.Platforms.PublishStatus.Published"/> rows for
    /// a platform by stamping each with the platform-deleted instant, keeping the
    /// rows as read-only history when the platform is deleted. Rows in other
    /// states are left untouched. Returns the number of rows orphaned.
    /// </summary>
    Task<int> OrphanPublishedByPlatformAsync(
        string platformId,
        CancellationToken cancellationToken);
}
