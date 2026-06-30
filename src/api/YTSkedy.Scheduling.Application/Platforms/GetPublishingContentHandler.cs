using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Reads row-level publishing content for one calendar event and platform. A
/// not-published active row renders a current preview, while in-progress,
/// published, and orphaned published rows return the stored content snapshot.
/// Full event details do not call this handler, so a bad template affects only
/// this on-demand read.
/// </summary>
public sealed class GetPublishingContentHandler(
    ICalendarEventReader calendarEvents,
    IPlatformReader platforms,
    IPlatformPublicationReader publications,
    PublishingContentRenderer contentRenderer)
{
    public async Task<GetPublishingContentResult> HandleAsync(
        GetPublishingContentQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.CalendarEventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query.PlatformId);

        var calendarEvent = await calendarEvents.GetByIdAsync(
            query.CalendarEventId,
            cancellationToken);
        if (calendarEvent is null)
        {
            return GetPublishingContentResult.ForStatus(
                GetPublishingContentStatus.CalendarEventNotFound);
        }

        var publication = await publications.GetAsync(
            query.CalendarEventId,
            query.PlatformId,
            cancellationToken);
        if (publication?.ContentSnapshot is not null)
        {
            return GetPublishingContentResult.Snapshot(publication.ContentSnapshot);
        }

        if (publication is not null && publication.Status != PublishStatus.NotPublished)
        {
            return GetPublishingContentResult.ForStatus(
                GetPublishingContentStatus.PreviewUnavailable);
        }

        var platform = await platforms.GetAsync(query.PlatformId, cancellationToken);
        if (platform is null)
        {
            return GetPublishingContentResult.ForStatus(
                GetPublishingContentStatus.PlatformNotFound);
        }

        var renderResult = await contentRenderer.RenderAsync(
            platform,
            calendarEvent,
            cancellationToken);

        return renderResult.Status switch
        {
            RenderContentStatus.Rendered =>
                GetPublishingContentResult.Preview(renderResult.Content!),
            RenderContentStatus.TemplateNotFound =>
                GetPublishingContentResult.ForStatus(
                    GetPublishingContentStatus.TemplateNotFound),
            RenderContentStatus.EmptyTitle =>
                GetPublishingContentResult.ForStatus(GetPublishingContentStatus.EmptyTitle),
            _ => GetPublishingContentResult.ForStatus(
                GetPublishingContentStatus.PreviewUnavailable)
        };
    }
}
