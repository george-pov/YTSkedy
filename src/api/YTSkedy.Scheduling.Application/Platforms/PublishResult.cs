using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Result of the publish use case. On <see cref="PublishOutcome.Published"/> it
/// carries the platform name and type plus the recorded
/// <see cref="ExternalResourceId"/> and <see cref="PublishedUtc"/> the success
/// response returns. For every other outcome those fields are null and the host
/// maps the outcome to the matching status code.
/// </summary>
public sealed record PublishResult(
    PublishOutcome Outcome,
    string? PlatformName,
    PlatformType? PlatformType,
    string? ExternalResourceId,
    DateTimeOffset? PublishedUtc)
{
    public static PublishResult ForOutcome(PublishOutcome outcome) =>
        new(outcome, null, null, null, null);

    public static PublishResult Published(
        string platformName,
        PlatformType platformType,
        string externalResourceId,
        DateTimeOffset publishedUtc) =>
        new(
            PublishOutcome.Published,
            platformName,
            platformType,
            externalResourceId,
            publishedUtc);
}
