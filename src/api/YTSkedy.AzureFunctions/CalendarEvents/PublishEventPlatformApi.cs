using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// HTTP boundary for publishing a calendar event to a selected platform. Hosts
/// <c>POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish</c>
/// under the Azure Functions <c>/api</c> prefix with the <c>CalendarEvents.Write</c>
/// scope. The request body is empty; both ids come from the
/// route. The boundary owns mapping the publish outcome to a status code.
/// </summary>
public sealed class PublishEventPlatformApi(PublishHandler publishHandler)
{
    [Function("PublishEventPlatform")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> PublishAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "calendar-events/{calendarEventId}/platforms/{platformId}/publish")]
        HttpRequest request,
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        var result = await publishHandler.HandleAsync(
            new PublishCommand(calendarEventId, platformId),
            cancellationToken);

        return ToResult(result, calendarEventId, platformId);
    }

    /// <summary>
    /// Maps a publish outcome to its HTTP result. Published is 200 with the
    /// publish body; not-found is 404; past start is 400; invalid publishing
    /// content, already-published, publish-in-progress, and platform-deleted
    /// are 409; an unsupported provider is 501; a provider failure is 502; and
    /// a finalize failure is 500.
    /// </summary>
    internal static IActionResult ToResult(
        PublishResult result,
        string calendarEventId,
        string platformId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            PublishResultStatus.Published =>
                new OkObjectResult(CalendarEventsApi.ToEventPlatformResponse(result.Platform!)),
            PublishResultStatus.EventNotFound =>
                new NotFoundObjectResult($"Calendar event '{calendarEventId}' was not found."),
            PublishResultStatus.PlatformNotFound =>
                new NotFoundObjectResult($"Platform '{platformId}' was not found."),
            PublishResultStatus.PastStart =>
                new BadRequestObjectResult("The calendar event start must be in the future."),
            PublishResultStatus.InvalidPublishingContent =>
                new ConflictObjectResult(
                    "The resolved publishing content must have a non-empty title and no " +
                    "unresolved placeholders."),
            PublishResultStatus.AlreadyPublished =>
                new ConflictObjectResult(
                    $"Calendar event '{calendarEventId}' is already published to platform '{platformId}'."),
            PublishResultStatus.PublishInProgress =>
                new ConflictObjectResult(
                    $"A publish of calendar event '{calendarEventId}' to platform '{platformId}' " +
                    "is already in progress."),
            PublishResultStatus.PlatformDeleted =>
                new ConflictObjectResult(
                    $"Platform '{platformId}' was deleted; its publication is read-only history."),
            PublishResultStatus.ProviderNotSupported =>
                new ObjectResult($"Publishing to platform '{platformId}' is not supported.")
                {
                    StatusCode = StatusCodes.Status501NotImplemented
                },
            PublishResultStatus.ProviderFailed =>
                new ObjectResult(
                    $"Publishing calendar event '{calendarEventId}' to platform '{platformId}' " +
                    "failed at the provider.")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                },
            PublishResultStatus.FinalizeFailed =>
                new ObjectResult(
                    $"Publishing calendar event '{calendarEventId}' to platform '{platformId}' " +
                    "failed while finalizing publication state.")
                {
                    StatusCode = StatusCodes.Status500InternalServerError
                },
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

}
