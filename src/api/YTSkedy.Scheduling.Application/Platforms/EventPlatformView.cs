using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// One row of the event-platform listing: a platform paired with its publication
/// state for a single calendar event. Active platforms with no publication row
/// are reported as <see cref="PublishStatus.NotPublished"/> with no external
/// resource. Orphaned history rows carry the deleted platform's name and type
/// from the stored row and set <see cref="PlatformDeletedUtc"/>.
/// <see cref="CanPublish"/>, <see cref="CanDeletePublication"/>, and
/// <see cref="CanPreviewPublishingContent"/> are the precomputed action flags
/// from <see cref="PlatformActionPolicy"/>.
/// </summary>
public sealed record EventPlatformView(
    string PlatformId,
    string PlatformName,
    PlatformType PlatformType,
    PublishStatus Status,
    string? ExternalResourceId,
    DateTimeOffset? PublishedUtc,
    DateTimeOffset? PlatformDeletedUtc,
    bool CanPublish,
    bool CanDeletePublication,
    bool CanPreviewPublishingContent,
    ThumbnailPublishStatus? ThumbnailStatus = null);
