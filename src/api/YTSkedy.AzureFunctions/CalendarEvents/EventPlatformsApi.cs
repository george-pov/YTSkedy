using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// HTTP boundary for the publication state of a calendar event across platforms.
/// Hosts <c>GET /api/calendar-events/{calendarEventId}/platforms</c> under the
/// Azure Functions <c>/api</c> prefix, reusing the <c>CalendarEvents.Read</c>
/// bearer-token scope. A missing calendar event maps to <c>404 Not Found</c>; a
/// found event returns one item per active platform (with computed
/// <c>NotPublished</c> state when no row exists) plus orphaned history rows.
/// </summary>
public class EventPlatformsApi(ListPlatformsForEventHandler listHandler)
{
    [Function("ListEventPlatforms")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> ListEventPlatformsAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "calendar-events/{calendarEventId}/platforms")]
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var views = await listHandler.HandleAsync(calendarEventId, cancellationToken);

        return views is null
            ? new NotFoundObjectResult($"Calendar event '{calendarEventId}' was not found.")
            : new OkObjectResult(ToListResponse(calendarEventId, views));
    }

    public static EventPlatformListResponse ToListResponse(
        string calendarEventId,
        IReadOnlyList<EventPlatformView> views)
    {
        ArgumentNullException.ThrowIfNull(views);

        return new EventPlatformListResponse(
            calendarEventId,
            views.Select(ToEventPlatformResponse).ToArray());
    }

    public static EventPlatformResponse ToEventPlatformResponse(EventPlatformView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new EventPlatformResponse(
            view.PlatformId,
            view.PlatformName,
            view.PlatformType.ToString(),
            view.Status.ToString(),
            view.ExternalResourceId,
            view.PublishedUtc,
            view.PlatformDeletedUtc,
            view.CanPublish);
    }
}
