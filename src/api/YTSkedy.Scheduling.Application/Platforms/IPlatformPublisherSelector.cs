using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Selects the <see cref="IPlatformPublisher"/> registered for a
/// <see cref="PlatformType"/>. Returns null when no provider serves the type, so
/// the publish use case can map that to <c>501 Not Implemented</c> without
/// referencing concrete providers.
/// </summary>
public interface IPlatformPublisherSelector
{
    IPlatformPublisher? Find(PlatformType type);
}
