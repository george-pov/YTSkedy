using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;

namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Provider port that deletes one external resource created by platform
/// publishing. A concrete deleter serves one <see cref="PlatformType"/>, so the
/// application use case stays provider-neutral.
/// </summary>
public interface IPlatformPublicationDeleter : IPlatformTypeAdapter
{
    Task<PublicationDeleteResult> DeleteAsync(
        PublicationDeleteRequest request,
        CancellationToken cancellationToken);
}
