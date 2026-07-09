using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Provider port that publishes one calendar event to one external platform. A
/// concrete publisher serves a single <see cref="PlatformType"/> and is selected
/// by that type, so generic publish code never references provider SDKs. The
/// implementation lives in infrastructure and returns a provider-neutral
/// <see cref="PlatformPublishResult"/>. A failed external call throws
/// <see cref="PlatformPublishException"/> so the caller can release its
/// attempt and surface an upstream failure.
/// </summary>
public interface IPlatformPublisher : IPlatformTypeAdapter
{
    Task<PlatformPublishResult> PublishAsync(
        PlatformPublishRequest request,
        CancellationToken cancellationToken);
}
