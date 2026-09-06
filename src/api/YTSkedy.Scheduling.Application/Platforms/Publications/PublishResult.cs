using YTSkedy.Scheduling.Application.Platforms.EventPlatforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public sealed record PublishResult(
    PublishResultStatus Status,
    EventPlatformView? Platform,
    PublicationFailure? Failure = null)
{
    public static PublishResult ForStatus(PublishResultStatus status) =>
        new(status, null);

    public static PublishResult Published(EventPlatformView platform)
    {
        ArgumentNullException.ThrowIfNull(platform);

        return new(PublishResultStatus.Published, platform);
    }

    public static PublishResult Failed(PublicationFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);

        return new(PublishResultStatus.Failed, null, failure);
    }
}
