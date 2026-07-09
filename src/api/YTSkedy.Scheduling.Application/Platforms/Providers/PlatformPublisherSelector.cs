using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.Providers;

/// <summary>
/// Indexes the registered <see cref="IPlatformPublisher"/> instances by
/// <see cref="IPlatformPublisher.Type"/>. The first publisher registered for a
/// type wins; <see cref="Find"/> returns null for any type with no registered
/// provider.
/// </summary>
public sealed class PlatformPublisherSelector : IPlatformPublisherSelector
{
    private readonly IReadOnlyDictionary<PlatformType, IPlatformPublisher> _publishersByType;

    public PlatformPublisherSelector(IEnumerable<IPlatformPublisher> publishers)
    {
        ArgumentNullException.ThrowIfNull(publishers);

        _publishersByType = publishers
            .GroupBy(publisher => publisher.Type)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public IPlatformPublisher? Find(PlatformType type) =>
        _publishersByType.GetValueOrDefault(type);
}
