namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// One platform entry in the event-platform listing. Carries the platform id and
/// type a client needs to drive the publish route, the publication status, and
/// the precomputed row action flags. Orphaned history rows set
/// <c>platformDeletedUtc</c> and report both action flags as false.
/// </summary>
internal sealed record EventPlatformResponse(
    string PlatformId,
    string PlatformName,
    string PlatformType,
    string Status,
    string? ExternalResourceId,
    string? ThumbnailStatus,
    DateTimeOffset? PublishedUtc,
    DateTimeOffset? PlatformDeletedUtc,
    bool CanPublish,
    bool CanDeletePublication,
    bool CanPreviewPublishingContent);
