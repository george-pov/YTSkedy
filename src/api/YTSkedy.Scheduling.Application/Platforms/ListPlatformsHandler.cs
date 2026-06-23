using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Lists platform views, optionally scoped to a single provider type. Filtering
/// by type is delegated to the reader, so the handler forwards the query and
/// returns the views unchanged.
/// </summary>
public sealed class ListPlatformsHandler(IPlatformReader platforms)
{
    public async Task<IReadOnlyList<PlatformView>> HandleAsync(
        ListPlatformsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await platforms.ListAsync(query.Type, cancellationToken);
    }
}
