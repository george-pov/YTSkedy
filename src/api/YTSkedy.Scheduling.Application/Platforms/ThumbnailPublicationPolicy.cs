using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public static class ThumbnailPublicationPolicy
{
    public static bool SupportsThumbnails(PlatformType platformType) =>
        platformType == PlatformType.YouTube;

    public static ThumbnailPublishStatus? InitialStatusFor(PlatformType platformType) =>
        SupportsThumbnails(platformType)
            ? ThumbnailPublishStatus.NotConfigured
            : null;
}
