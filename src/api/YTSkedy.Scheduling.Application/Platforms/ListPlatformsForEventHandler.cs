using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Lists every active registered platform for one calendar event with its
/// publication state, plus orphaned history rows for platforms that were deleted
/// after publishing. The calendar event is loaded first so a missing event maps
/// to <c>404 Not Found</c> at the boundary; the handler returns null in that
/// case. An active platform with no publication row is reported as computed
/// <see cref="PublishStatus.NotPublished"/>, so no row is created just to read
/// state. Each item's action flag comes from <see cref="PlatformActionPolicy"/>.
/// </summary>
public sealed class ListPlatformsForEventHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications)
{
    public async Task<IReadOnlyList<EventPlatformView>?> HandleAsync(
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(calendarEventId);

        var calendarEvent = await calendarEvents.GetByIdAsync(calendarEventId, cancellationToken);

        if (calendarEvent is null)
        {
            return null;
        }

        var activePlatforms = await platforms.ListAsync(null, cancellationToken);
        var publicationRows = await publications.ListByEventAsync(calendarEventId, cancellationToken);

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
