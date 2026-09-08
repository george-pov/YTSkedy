using YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms.EventPlatforms;

/// <summary>
/// Projects active platforms and stored publication rows for one calendar event
/// into the event-platform view list shared by the event-platform listing and
/// the calendar event details read model. An active platform with no publication
/// row is reported as a computed <see cref="PublishStatus.NotPublished"/> item,
/// so no row is created just to read state. Orphaned rows for platforms that no
/// longer exist are appended as read-only history using the name and type copied
/// onto the row at publish time. Each item's action flag comes from
/// <see cref="PlatformActionPolicy"/>. The caller is responsible for loading the
/// calendar event and deciding the missing-event outcome.
/// </summary>
public static class EventPlatformMapper
{
    public static IReadOnlyList<EventPlatformView> Map(
        Domain.CalendarEvents.CalendarEventView calendarEvent,
        IReadOnlyList<PlatformView> activePlatforms,
        IReadOnlyList<PlatformPublication> publicationRows,
        DateTimeOffset now,
        TimeSpan staleAfter)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        ArgumentNullException.ThrowIfNull(activePlatforms);
        ArgumentNullException.ThrowIfNull(publicationRows);

        var publicationsByPlatform = publicationRows.ToDictionary(
            publication => publication.PlatformId,
            StringComparer.Ordinal);

        var items = new List<EventPlatformView>(activePlatforms.Count);

        foreach (var platform in activePlatforms)
        {
            publicationsByPlatform.TryGetValue(platform.PlatformId, out var publication);

            items.Add(publication is null
                ? MapNotPublished(calendarEvent, platform, now)
                : MapActive(calendarEvent, platform, publication, now, staleAfter));
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

            items.Add(MapOrphan(calendarEvent, publication, now, staleAfter));
        }

        return items;
    }

    public static EventPlatformView MapPublished(
        Domain.CalendarEvents.CalendarEventView calendarEvent,
        PlatformView platform,
        string externalResourceId,
        DateTimeOffset publishedUtc,
        DateTimeOffset now,
        ThumbnailPublishStatus? thumbnailStatus)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalResourceId);

        var isFuture = calendarEvent.ScheduledStartUtc > now;
        const PublishStatus status = PublishStatus.Published;

        return new EventPlatformView(
            platform.PlatformId,
            platform.Name,
            platform.Type,
            status,
            externalResourceId,
            publishedUtc,
            null,
            PlatformActionPolicy.CanPublish(status, isOrphaned: false, isFuture),
            PlatformActionPolicy.CanDeletePublication(
                status,
                isOrphaned: false,
                hasExternalResourceId: true,
                isFuture),
            PlatformActionPolicy.CanPreviewPublishingContent(
                status,
                isOrphaned: false,
                hasContentSnapshot: true),
            thumbnailStatus,
            publishedUtc,
            CanRecoverPublication: false);
    }

    public static EventPlatformView MapNotPublished(
        Domain.CalendarEvents.CalendarEventView calendarEvent,
        PlatformView platform,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        ArgumentNullException.ThrowIfNull(platform);

        var isFuture = calendarEvent.ScheduledStartUtc > now;
        const PublishStatus status = PublishStatus.NotPublished;

        return new EventPlatformView(
            platform.PlatformId,
            platform.Name,
            platform.Type,
            status,
            null,
            null,
            null,
            PlatformActionPolicy.CanPublish(status, isOrphaned: false, isFuture),
            PlatformActionPolicy.CanDeletePublication(
                status,
                isOrphaned: false,
                hasExternalResourceId: false,
                isFuture),
            PlatformActionPolicy.CanPreviewPublishingContent(
                status,
                isOrphaned: false,
                hasContentSnapshot: false),
            ThumbnailPublicationPolicy.InitialStatusFor(platform.Type),
            PublicationUpdatedUtc: null,
            CanRecoverPublication: false);
    }

    private static EventPlatformView MapActive(
        Domain.CalendarEvents.CalendarEventView calendarEvent,
        PlatformView platform,
        PlatformPublication publication,
        DateTimeOffset now,
        TimeSpan staleAfter)
    {
        var isFuture = calendarEvent.ScheduledStartUtc > now;
        var hasExternalResourceId = !string.IsNullOrWhiteSpace(publication.ExternalResourceId);
        var hasContentSnapshot = publication.ContentSnapshot is not null;

        return new EventPlatformView(
            platform.PlatformId,
            platform.Name,
            platform.Type,
            publication.Status,
            publication.ExternalResourceId,
            publication.PublishedUtc,
            publication.PlatformDeletedUtc,
            PlatformActionPolicy.CanPublish(
                publication.Status,
                publication.IsOrphaned,
                isFuture),
            PlatformActionPolicy.CanDeletePublication(
                publication.Status,
                publication.IsOrphaned,
                hasExternalResourceId,
                isFuture),
            PlatformActionPolicy.CanPreviewPublishingContent(
                publication.Status,
                publication.IsOrphaned,
                hasContentSnapshot),
            publication.ThumbnailStatus ??
            ThumbnailPublicationPolicy.InitialStatusFor(platform.Type),
            publication.UpdatedUtc,
            PlatformActionPolicy.CanRecoverPublication(
                publication.Status,
                publication.IsOrphaned,
                isFuture,
                publication.UpdatedUtc,
                now,
                staleAfter),
            publication.LastFailure);
    }

    private static EventPlatformView MapOrphan(
        Domain.CalendarEvents.CalendarEventView calendarEvent,
        PlatformPublication publication,
        DateTimeOffset now,
        TimeSpan staleAfter)
    {
        var isFuture = calendarEvent.ScheduledStartUtc > now;

        return new EventPlatformView(
            publication.PlatformId,
            publication.PlatformName,
            publication.PlatformType,
            publication.Status,
            publication.ExternalResourceId,
            publication.PublishedUtc,
            publication.PlatformDeletedUtc,
            PlatformActionPolicy.CanPublish(
                publication.Status,
                publication.IsOrphaned,
                isFuture),
            PlatformActionPolicy.CanDeletePublication(
                publication.Status,
                publication.IsOrphaned,
                !string.IsNullOrWhiteSpace(publication.ExternalResourceId),
                isFuture),
            PlatformActionPolicy.CanPreviewPublishingContent(
                publication.Status,
                publication.IsOrphaned,
                publication.ContentSnapshot is not null),
            publication.ThumbnailStatus ??
            ThumbnailPublicationPolicy.InitialStatusFor(publication.PlatformType),
            publication.UpdatedUtc,
            PlatformActionPolicy.CanRecoverPublication(
                publication.Status,
                publication.IsOrphaned,
                isFuture,
                publication.UpdatedUtc,
                now,
                staleAfter),
            publication.LastFailure);
    }
}
