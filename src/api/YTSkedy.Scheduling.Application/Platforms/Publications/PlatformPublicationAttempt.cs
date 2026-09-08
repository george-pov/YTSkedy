using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Input used to start a publication attempt for one calendar event and one
/// platform. The platform name, type, and publish settings are copied onto the
/// <c>Publishing</c> row so the attempt is described by the settings in effect
/// when it started and remains readable after the platform record changes or is
/// deleted. The content snapshot records the resolved title and description at
/// the moment the publish attempt starts.
/// </summary>
public sealed record PlatformPublicationAttempt(
    string CalendarEventId,
    string PlatformId,
    string PlatformName,
    PlatformType PlatformType,
    PublishSettings PublishSettings,
    ContentSnapshot ContentSnapshot,
    string AttemptId = "");
