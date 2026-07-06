using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Indexes registered thumbnail publishers by provider type. Missing entries
/// mean the provider does not support thumbnail application in this feature.
/// </summary>
public sealed class ThumbnailPublisherSelector : IThumbnailPublisherSelector
{
    private readonly IReadOnlyDictionary<PlatformType, IThumbnailPublisher> publishersByType;

    public ThumbnailPublisherSelector(IEnumerable<IThumbnailPublisher> publishers)
    {
        ArgumentNullException.ThrowIfNull(publishers);

        publishersByType = publishers
            .GroupBy(publisher => publisher.Type)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public IThumbnailPublisher? Find(PlatformType type) =>
        publishersByType.GetValueOrDefault(type);
}
