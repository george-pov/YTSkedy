namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Decides which actions a publication state allows. Centralizing the rules keeps
/// the event-platform listing, the publish use case, and the platform delete
/// guard consistent. The policy is intentionally state-only: event timing and
/// provider-required content are validated separately at publish time.
/// </summary>
public static class PlatformActionPolicy
{
    /// <summary>
    /// True when a publish may be attempted. A publish is allowed only for an
    /// active platform whose publication is <see cref="PublishStatus.NotPublished"/>.
    /// Orphaned history, in-flight (<see cref="PublishStatus.Publishing"/>), and
    /// completed (<see cref="PublishStatus.Published"/>) publications are not
    /// publishable in this iteration.
    /// </summary>
    public static bool CanPublish(PublishStatus status, bool isOrphaned) =>
        !isOrphaned && status == PublishStatus.NotPublished;

    /// <summary>
    /// True when a publication blocks deleting its platform. Deletion is blocked
    /// while a publish is in progress so a provider call cannot race a delete;
    /// published rows do not block because they are preserved as orphan history.
    /// </summary>
    public static bool BlocksPlatformDelete(PublishStatus status) =>
        status == PublishStatus.Publishing;
}
