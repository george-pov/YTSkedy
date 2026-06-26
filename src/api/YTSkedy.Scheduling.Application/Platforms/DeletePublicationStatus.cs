namespace YTSkedy.Scheduling.Application.Platforms;

public enum DeletePublicationStatus
{
    Deleted = 0,
    AlreadyNotPublished = 1,
    EventNotFound = 2,
    PlatformNotFound = 3,
    Orphaned = 4,
    PastStart = 5,
    MissingExternalResourceId = 6,
    TargetMismatch = 7,
    PublishInProgress = 8,
    ProviderNotSupported = 9,
    ProviderStateConflict = 10,
    ProviderFailed = 11,
    RowChanged = 12
}
