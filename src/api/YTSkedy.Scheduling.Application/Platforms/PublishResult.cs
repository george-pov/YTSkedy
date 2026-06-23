using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Result of the publish use case. On <see cref="PublishResultStatus.Published"/>
/// it carries the platform name and type plus the recorded
/// <see cref="ExternalResourceId"/> and <see cref="PublishedUtc"/> the success
/// response returns. For every other status those fields are null and the host
/// maps the status to the matching status code.
/// </summary>
public sealed record PublishResult(
    PublishResultStatus Status,
    string? PlatformName,
    PlatformType? PlatformType,
    string? ExternalResourceId,
    DateTimeOffset? PublishedUtc)
{
    public static PublishResult ForStatus(PublishResultStatus status) =>
        new(status, null, null, null, null);

    public static PublishResult Published(
        string platformName,
        PlatformType platformType,
        string externalResourceId,
        DateTimeOffset publishedUtc) =>
        new(
            PublishResultStatus.Published,
            platformName,
            platformType,
            externalResourceId,
            publishedUtc);
}
