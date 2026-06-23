namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// One platform entry in the event-platform listing. Carries the platform id and
/// type a client needs to drive the publish route, the publication status, and
/// the precomputed <c>canPublish</c> action flag. Orphaned history rows set
/// <c>platformDeletedUtc</c> and report <c>canPublish: false</c>.
/// </summary>
public sealed record EventPlatformResponse(
    string PlatformId,
    string PlatformName,
    string PlatformType,
    string Status,
    string? ExternalResourceId,
    DateTimeOffset? PublishedUtc,
    DateTimeOffset? PlatformDeletedUtc,
    bool CanPublish);
