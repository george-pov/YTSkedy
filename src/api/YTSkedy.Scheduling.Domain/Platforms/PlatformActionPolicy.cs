namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Decides which actions a publication state allows. Centralizing the rules keeps
/// the event-platform listing, the publish use case, the publication delete use
/// case, and the platform delete guard consistent.
/// </summary>
public static class PlatformActionPolicy
{
    /// <summary>
    /// True when a publish may be attempted. A publish is allowed only for a
    /// future event on an active platform whose publication is
    /// <see cref="PublishStatus.NotPublished"/> or a retryable
    /// <see cref="PublishStatus.Failed"/> state. Orphaned history, in-flight,
    /// and completed publications are not publishable.
    /// </summary>
    public static bool CanPublish(
        PublishStatus status,
        bool isOrphaned,
        bool isFuture) =>
        !isOrphaned &&
        isFuture &&
        status is PublishStatus.NotPublished or PublishStatus.Failed;

    /// <summary>
    /// True when a completed platform publication may be deleted. Only a future,
    /// active, published row with a provider resource id is deletable.
    /// </summary>
    public static bool CanDeletePublication(
        PublishStatus status,
        bool isOrphaned,
        bool hasExternalResourceId,
        bool isFuture) =>
        !isOrphaned &&
        isFuture &&
        hasExternalResourceId &&
        status == PublishStatus.Published;

    /// <summary>
    /// True when an active future publication has remained in Publishing for
    /// at least the configured stale interval. The inclusive boundary permits
    /// recovery at exactly <paramref name="staleAfter"/>.
    /// </summary>
    public static bool CanRecoverPublication(
        PublishStatus status,
        bool isOrphaned,
        bool isFuture,
        DateTimeOffset? publicationUpdatedUtc,
        DateTimeOffset now,
        TimeSpan staleAfter) =>
        status == PublishStatus.Publishing &&
        !isOrphaned &&
        isFuture &&
        publicationUpdatedUtc is not null &&
        publicationUpdatedUtc.Value <= now - staleAfter;

    /// <summary>
    /// True when row-level publishing content can be read. Active
    /// <see cref="PublishStatus.NotPublished"/> rows can render a current
    /// preview. In-progress, failed, and completed rows can read the stored
    /// content snapshot. Orphaned history can read a completed snapshot but
    /// cannot render a current preview.
    /// </summary>
    public static bool CanPreviewPublishingContent(
        PublishStatus status,
        bool isOrphaned,
        bool hasContentSnapshot) =>
        status switch
        {
            PublishStatus.NotPublished => !isOrphaned,
            PublishStatus.Publishing => !isOrphaned && hasContentSnapshot,
            PublishStatus.Published => hasContentSnapshot,
            PublishStatus.Failed => !isOrphaned && hasContentSnapshot,
            _ => false
        };

    /// <summary>
    /// True when a publication blocks deleting its platform. Deletion is blocked
    /// while a publish is in progress so a provider call cannot race a delete;
    /// published rows do not block because they are preserved as orphan history.
    /// </summary>
    public static bool BlocksPlatformDelete(PublishStatus status) =>
        status == PublishStatus.Publishing;
}
