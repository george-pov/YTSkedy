using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Indexes registered publication cleanup adapters by provider type. Returning
/// null lets the use case map unsupported providers to <c>501 Not Implemented</c>
/// without referencing infrastructure adapters.
/// </summary>
public sealed class PublicationDeleterSelector : IPublicationDeleterSelector
{
    private readonly IReadOnlyDictionary<PlatformType, IPlatformPublicationDeleter> _deletersByType;

    public PublicationDeleterSelector(IEnumerable<IPlatformPublicationDeleter> deleters)
    {
        ArgumentNullException.ThrowIfNull(deleters);

        _deletersByType = deleters
            .GroupBy(deleter => deleter.Type)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public IPlatformPublicationDeleter? Find(PlatformType type) =>
        _deletersByType.GetValueOrDefault(type);
}
