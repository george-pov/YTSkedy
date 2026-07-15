namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public enum RecoverPublicationStatus
{
    Recovered,
    EventNotFound,
    PlatformNotFound,
    PublicationNotFound,
    PlatformDeleted,
    PastStart,
    NotPublishing,
    NotStale,
    RowChanged
}
