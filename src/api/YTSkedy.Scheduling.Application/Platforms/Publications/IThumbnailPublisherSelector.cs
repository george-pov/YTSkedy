using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public interface IThumbnailPublisherSelector
{
    IThumbnailPublisher? Find(PlatformType type);
}
