using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Provider-neutral input for publishing one calendar event to one platform. The
/// title and optional description are the resolved publish content, the scheduled
/// start is the stored UTC instant, and <see cref="PublishSettings"/> carries
/// the selected platform's provider settings. The calendar event and platform
/// ids are included for provider-side logging and idempotency.
/// </summary>
public sealed record PlatformPublishRequest(
    string CalendarEventId,
    string PlatformId,
    PublishSettings PublishSettings,
    string Title,
    string? Description,
    DateTimeOffset ScheduledStartUtc);
