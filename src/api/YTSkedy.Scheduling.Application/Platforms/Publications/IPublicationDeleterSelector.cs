using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

public interface IPublicationDeleterSelector
{
    IPlatformPublicationDeleter? Find(PlatformType type);
}
