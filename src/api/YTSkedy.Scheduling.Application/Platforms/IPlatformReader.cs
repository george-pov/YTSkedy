using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

public interface IPlatformReader
{
    /// <summary>
    /// Reads platform views. When <paramref name="type"/> is supplied the result
    /// is scoped to that provider type; when it is null platforms of every type
    /// are returned. The returned order is not significant.
    /// </summary>
    Task<IReadOnlyList<PlatformView>> ListAsync(
        PlatformType? type,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads only the ids of currently configured platforms.
    /// </summary>
    Task<IReadOnlySet<string>> ListIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reads a single platform view by id, or null when no platform has the id.
    /// </summary>
    Task<PlatformView?> GetAsync(
        string platformId,
        CancellationToken cancellationToken);
}
