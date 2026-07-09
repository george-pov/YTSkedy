namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public interface IPublicationHistoryWriter
{
    /// <summary>
    /// Orphans the <see cref="Domain.Platforms.PublishStatus.Published"/> rows
    /// for a platform by stamping each with the platform-deleted instant,
    /// keeping the rows as read-only history when the platform is deleted. Rows
    /// in other states are left untouched. Returns the number of rows orphaned.
    /// </summary>
    Task<int> OrphanPublishedByPlatformAsync(
        string platformId,
        CancellationToken cancellationToken);
}
