using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public interface IPublicationAttemptWriter
{
    /// <summary>
    /// Starts a publication row by creating it directly as
    /// <see cref="Domain.Platforms.PublishStatus.Publishing"/> with the platform
    /// name, type, and publish settings copied from the attempt. The write is
    /// conditional on the row not already existing or being a current active
    /// <see cref="Domain.Platforms.PublishStatus.Failed"/> row. A failed row is
    /// conditionally replaced for retry. Other existing rows are conflicts.
    /// </summary>
    Task<StartPublicationResult> StartPublishingAsync(
        PlatformPublicationAttempt attempt,
        CancellationToken cancellationToken);

    Task<SaveExternalResourceIdResult> SaveExternalResourceIdAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken);

    Task<RecoverStalePublishingResult> RecoverStalePublishingAsync(
        string calendarEventId,
        string platformId,
        DateTimeOffset expectedUpdatedUtc,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases an in-progress attempt by removing the <c>Publishing</c> row,
    /// returning the event/platform pair to the computed
    /// <see cref="Domain.Platforms.PublishStatus.NotPublished"/> state. Caught
    /// publish failures use <see cref="MarkFailedAsync"/> instead. This lower-
    /// level operation is reserved for explicit release workflows. A missing
    /// row is treated as already released.
    /// </summary>
    Task ReleasePublishingAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an in-progress publication row
    /// <see cref="Domain.Platforms.PublishStatus.Published"/> after the provider
    /// call succeeds, recording the provider <paramref name="externalResourceId"/>
    /// and the publish instant. Returns the recorded publish instant, or null
    /// when the current row is missing, orphaned, or no longer publishing.
    /// </summary>
    Task<DateTimeOffset?> MarkPublishedAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Conditionally marks only the current <c>Publishing</c> row as
    /// <see cref="Domain.Platforms.PublishStatus.Failed"/> and retains the
    /// provider id when one is known. The secret-safe failure summary is stored
    /// for operator troubleshooting. Another writer's state is never overwritten.
    /// </summary>
    Task<MarkFailedResult> MarkFailedAsync(
        string calendarEventId,
        string platformId,
        string? externalResourceId,
        PublicationFailure failure,
        CancellationToken cancellationToken);
}
