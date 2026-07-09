using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Indexes registered platform-type adapters by <see cref="IPlatformTypeAdapter.Type"/>.
/// The first adapter registered for a type wins; <see cref="Find"/> returns null
/// when no adapter serves the requested type.
/// </summary>
public sealed class PlatformTypeAdapterSelector<TAdapter> :
    IPlatformTypeAdapterSelector<TAdapter>
    where TAdapter : IPlatformTypeAdapter
{
    private readonly IReadOnlyDictionary<PlatformType, TAdapter> adaptersByType;

    public PlatformTypeAdapterSelector(IEnumerable<TAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);

        adaptersByType = adapters
            .GroupBy(adapter => adapter.Type)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public TAdapter? Find(PlatformType type) =>
        adaptersByType.GetValueOrDefault(type);
}
