using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Input used to reserve a publication row for one calendar event and one
/// platform. The platform name, type, and publish settings are copied onto the
/// reserved row so the attempt is described by the settings used at reservation
/// time and remains readable after the platform record changes or is deleted.
/// Only non-secret publish settings are carried; credential material is resolved
/// outside storage.
/// </summary>
public sealed record PlatformPublicationReservation(
    string CalendarEventId,
    string PlatformId,
    string PlatformName,
    PlatformType PlatformType,
    PublishSettings PublishSettings);
