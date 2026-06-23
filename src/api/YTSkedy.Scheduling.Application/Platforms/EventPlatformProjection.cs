using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Projects active platforms and stored publication rows for one calendar event
/// into the event-platform view list shared by the event-platform listing and
/// the calendar event detail read model. An active platform with no publication
/// row is reported as a computed <see cref="PublishStatus.NotPublished"/> item,
/// so no row is created just to read state. Orphaned rows for platforms that no
/// longer exist are appended as read-only history using the name and type copied
/// onto the row at publish time. Each item's action flag comes from
/// <see cref="PlatformActionPolicy"/>. The caller is responsible for loading the
/// calendar event and deciding the missing-event outcome.
/// </summary>
internal static class EventPlatformProjection
{
    internal static IReadOnlyList<EventPlatformView> Project(
        IReadOnlyList<PlatformView> activePlatforms,
        IReadOnlyList<PlatformPublication> publicationRows)
    {
        ArgumentNullException.ThrowIfNull(activePlatforms);
        ArgumentNullException.ThrowIfNull(publicationRows);

        var publicationsByPlatform = publicationRows.ToDictionary(
            publication => publication.PlatformId,
            StringComparer.Ordinal);

        var items = new List<EventPlatformView>(activePlatforms.Count);

        foreach (var platform in activePlatforms)
        {
            publicationsByPlatform.TryGetValue(platform.PlatformId, out var publication);

            // A missing row is the normal NotPublished state, so it is computed
            // rather than read from storage.
            var status = publication?.Status ?? PublishStatus.NotPublished;
            var isOrphaned = publication?.IsOrphaned ?? false;

            items.Add(new EventPlatformView(
                platform.PlatformId,
                platform.Name,
                platform.Type,
                status,
                publication?.ExternalResourceId,
                publication?.PublishedUtc,
                publication?.PlatformDeletedUtc,
                PlatformActionPolicy.CanPublish(status, isOrphaned)));
        }

        var activePlatformIds = activePlatforms
            .Select(platform => platform.PlatformId)
            .ToHashSet(StringComparer.Ordinal);

        // Orphaned rows describe publishing history for platforms that no longer
        // exist, so they are appended using the platform name and type copied onto
        // the row at publish time.
        foreach (var publication in publicationRows)
        {
            if (!publication.IsOrphaned || activePlatformIds.Contains(publication.PlatformId))
            {
                continue;
            }

            items.Add(new EventPlatformView(
                publication.PlatformId,
                publication.PlatformName,
                publication.PlatformType,
                publication.Status,
                publication.ExternalResourceId,
                publication.PublishedUtc,
                publication.PlatformDeletedUtc,
                PlatformActionPolicy.CanPublish(publication.Status, publication.IsOrphaned)));
        }

        return items;
    }
}
