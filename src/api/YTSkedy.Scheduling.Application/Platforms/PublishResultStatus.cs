namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Result status of the publish use case as seen by the HTTP host. The host maps
/// each value to a status code: <c>Published</c> to 200; <c>EventNotFound</c> and
/// <c>PlatformNotFound</c> to 404; <c>PastStart</c> and <c>MissingEnglishTitle</c>
/// to 400; <c>AlreadyPublished</c>, <c>PublishInProgress</c>, and
/// <c>PlatformDeleted</c> to 409; <c>ProviderNotSupported</c> to 501;
/// <c>ProviderFailed</c> to 502; and <c>FinalizeFailed</c> to 500.
/// </summary>
public enum PublishResultStatus
{
    Published,
    EventNotFound,
    PlatformNotFound,
    PastStart,
    MissingEnglishTitle,
    AlreadyPublished,
    PublishInProgress,
    PlatformDeleted,
    ProviderNotSupported,
    ProviderFailed,
    FinalizeFailed
}
