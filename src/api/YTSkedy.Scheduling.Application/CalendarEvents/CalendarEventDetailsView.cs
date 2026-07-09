using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.CalendarEvents;

/// <summary>
/// Read model for the calendar event details use case: the provider-neutral
/// calendar event paired with its per-platform publication state. The calendar
/// event itself carries no publish status; <see cref="Platforms"/> is composed
/// at read time from active platforms and stored publication rows. Active
/// platforms with no row are reported as computed
/// <see cref="Domain.Platforms.PublishStatus.NotPublished"/>, and orphaned
/// history rows for deleted platforms are included as read-only entries.
/// </summary>
public sealed record CalendarEventDetailsView(
    CalendarEventView Event,
    bool CanUpdate,
    bool CanDelete,
    IReadOnlyList<EventPlatformView> Platforms,
    Thumbnail? Thumbnail = null,
    bool CanUpdateThumbnail = true);
