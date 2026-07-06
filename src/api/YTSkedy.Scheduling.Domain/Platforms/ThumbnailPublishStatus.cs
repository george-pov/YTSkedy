namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Secondary thumbnail application state for a provider publication. The
/// primary publish state remains <see cref="PublishStatus"/>; this status only
/// describes whether a configured calendar-event thumbnail was applied after
/// the provider resource already existed.
/// </summary>
public enum ThumbnailPublishStatus
{
    NotConfigured = 0,
    Applied = 1,
    Failed = 2
}
