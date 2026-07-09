using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Providers;

public interface IPlatformTypeAdapterSelector<out TAdapter>
    where TAdapter : IPlatformTypeAdapter
{
    TAdapter? Find(PlatformType type);
}
