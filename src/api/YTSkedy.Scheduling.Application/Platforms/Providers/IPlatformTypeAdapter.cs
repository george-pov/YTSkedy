using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Provider adapter registered for exactly one platform type.
/// </summary>
public interface IPlatformTypeAdapter
{
    PlatformType Type { get; }
}
