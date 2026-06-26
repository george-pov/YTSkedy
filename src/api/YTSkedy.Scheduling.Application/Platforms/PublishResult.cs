namespace YTSkedy.Scheduling.Application.Platforms;

public sealed record PublishResult(
    PublishResultStatus Status,
    EventPlatformView? Platform)
{
    public static PublishResult ForStatus(PublishResultStatus status) =>
        new(status, null);

    public static PublishResult Published(EventPlatformView platform)
    {
        ArgumentNullException.ThrowIfNull(platform);

        return new(PublishResultStatus.Published, platform);
    }
}
