using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public interface IPublicationDeleterSelector
{
    IPlatformPublicationDeleter? Find(PlatformType type);
}
