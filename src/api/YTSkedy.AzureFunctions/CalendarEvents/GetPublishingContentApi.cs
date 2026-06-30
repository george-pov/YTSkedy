using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.Scheduling.Application.Platforms;

namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// HTTP boundary for reading row-level publishing content. The route returns a
/// rendered preview for active unpublished rows or the stored content snapshot
/// for rows where publishing has started.
/// </summary>
public sealed class GetPublishingContentApi(GetPublishingContentHandler handler)
{
    [Function("GetPublishingContent")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "calendar-events/{calendarEventId}/platforms/{platformId}/publishing-content")]
        HttpRequest request,
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetPublishingContentQuery(calendarEventId, platformId),
            cancellationToken);

        return ToResult(result, calendarEventId, platformId);
    }

    internal static IActionResult ToResult(
        GetPublishingContentResult result,
        string calendarEventId,
        string platformId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            GetPublishingContentStatus.Found => new OkObjectResult(
                new PublishingContentResponse(
                    result.Kind!.Value.ToString(),
                    result.Content!.Title,
                    result.Content.Description)),
            GetPublishingContentStatus.CalendarEventNotFound => new NotFoundObjectResult(
                $"Calendar event '{calendarEventId}' was not found."),
            GetPublishingContentStatus.PlatformNotFound => new NotFoundObjectResult(
                $"Platform '{platformId}' was not found."),
            GetPublishingContentStatus.PreviewUnavailable => new ConflictObjectResult(
                $"Publishing content is not available for calendar event '{calendarEventId}' " +
                $"and platform '{platformId}'."),
            GetPublishingContentStatus.TemplateNotFound => new ConflictObjectResult(
                $"One or more publishing content templates were not found for platform '{platformId}'."),
            GetPublishingContentStatus.EmptyTitle => new ConflictObjectResult(
                $"Publishing content for calendar event '{calendarEventId}' and platform " +
                $"'{platformId}' has an empty title."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }
}
