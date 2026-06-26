using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Provider-neutral input for deleting one external publication resource. The
/// publish settings come from the current active platform row and may contain
/// provider credentials; implementations must keep them out of logs and
/// responses.
/// </summary>
public sealed record PublicationDeleteRequest(
    string CalendarEventId,
    string PlatformId,
    PublishSettings PublishSettings,
    string ExternalResourceId);
