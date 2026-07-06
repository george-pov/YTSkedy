using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public interface IThumbnailPublisherSelector
{
    IThumbnailPublisher? Find(PlatformType type);
}
