using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Builds runtime template-token values from already-published platform
/// publications. The token name is the active platform's reference key and the
/// token value is that platform publication's provider-neutral external
/// resource id.
/// </summary>
internal static class PlatformReferenceTokenValues
{
    internal static IReadOnlyDictionary<string, string> From(
        IReadOnlyList<PlatformView> activePlatforms,
        IReadOnlyList<PlatformPublication> publicationRows)
    {
        ArgumentNullException.ThrowIfNull(activePlatforms);
        ArgumentNullException.ThrowIfNull(publicationRows);

        var publishedByPlatform = publicationRows
            .Where(publication =>
                publication.Status == PublishStatus.Published &&
                !publication.IsOrphaned &&
                !string.IsNullOrWhiteSpace(publication.ExternalResourceId))
            .ToDictionary(
                publication => publication.PlatformId,
                StringComparer.Ordinal);
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var platform in activePlatforms)
        {
            if (platform.ReferenceKey is null ||
                !publishedByPlatform.TryGetValue(platform.PlatformId, out var publication))
            {
                continue;
            }

            values[platform.ReferenceKey] = publication.ExternalResourceId!;
        }

        return values;
    }
}
