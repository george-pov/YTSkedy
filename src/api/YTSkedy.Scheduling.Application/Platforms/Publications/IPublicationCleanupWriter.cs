namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public interface IPublicationCleanupWriter
{
    /// <summary>
    /// Deletes a completed publication row after provider cleanup succeeds.
    /// Deletion is conditional on the row still being non-orphan
    /// <see cref="Domain.Platforms.PublishStatus.Published"/> with the same
    /// provider <paramref name="externalResourceId"/>. This is separate from
    /// <see cref="IPublicationAttemptWriter.ReleasePublishingAsync"/>, which
    /// only removes a current transient publishing attempt.
    /// </summary>
    Task<DeletePublishedResult> DeletePublishedAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken);
}
