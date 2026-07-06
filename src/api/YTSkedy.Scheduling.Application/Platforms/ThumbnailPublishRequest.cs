using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Provider-neutral input for applying a stored calendar-event thumbnail to an
/// already-created external provider resource.
/// </summary>
public sealed record ThumbnailPublishRequest(
    string CalendarEventId,
    string PlatformId,
    string ExternalResourceId,
    PublishSettings PublishSettings,
    ThumbnailContent ThumbnailContent);
