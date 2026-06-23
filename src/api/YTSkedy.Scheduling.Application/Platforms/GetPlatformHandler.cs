using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Reads a single platform by id. The reader owns the locate and the missing-row
/// outcome, so the handler forwards the id and returns the view or null
/// unchanged. The HTTP host maps null to <c>404 Not Found</c>.
/// </summary>
public sealed class GetPlatformHandler(IPlatformReader platforms)
{
    public async Task<PlatformView?> HandleAsync(
        string platformId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(platformId);

        return await platforms.GetAsync(platformId, cancellationToken);
    }
}
