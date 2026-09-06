namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Authoritative publish state for one calendar event and one
/// <see cref="Platform"/>. A row is created lazily when a publish is started, so
/// the absence of a row is read as <see cref="PublishStatus.NotPublished"/>. The
/// platform name and type are copied onto the row so the publication can still be
/// described after the platform is deleted. When a platform is deleted with
/// published rows the row is kept as orphan history and
/// <see cref="PlatformDeletedUtc"/> is set; <see cref="IsOrphaned"/> reports that
/// state.
/// </summary>
public sealed record PlatformPublication(
    string CalendarEventId,
    string PlatformId,
    string PlatformName,
    PlatformType PlatformType,
    PublishStatus Status,
    string? ExternalResourceId,
    DateTimeOffset? PublishedUtc,
    DateTimeOffset? PlatformDeletedUtc,
    DateTimeOffset UpdatedUtc,
    PublicationTargetSnapshot? TargetSnapshot = null,
    ContentSnapshot? ContentSnapshot = null,
    ThumbnailPublishStatus? ThumbnailStatus = null,
    PublicationFailure? LastFailure = null)
{
    /// <summary>
    /// True when the platform this publication targeted has been deleted, so the
    /// row is read-only history. Orphan rows cannot be published, retried, or
    /// deleted through normal user APIs.
    /// </summary>
    public bool IsOrphaned => PlatformDeletedUtc is not null;
}
