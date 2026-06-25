namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Publish state for one calendar event and one <see cref="Platform"/>. The
/// authoritative state lives in a <see cref="PlatformPublication"/> row, which is
/// created lazily when a publish is started. A missing row is read as
/// <see cref="NotPublished"/>, so that value is the normal representation of "no
/// publish has been attempted" and is rarely persisted on its own.
/// </summary>
public enum PublishStatus
{
    NotPublished = 0,
    Publishing = 1,
    Published = 2
}
