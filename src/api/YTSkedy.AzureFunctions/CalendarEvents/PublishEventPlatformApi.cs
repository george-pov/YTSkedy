using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// HTTP boundary for publishing a calendar event to a selected platform. Hosts
/// <c>POST /api/calendar-events/{calendarEventId}/platforms/{platformId}/publish</c>
/// under the Azure Functions <c>/api</c> prefix with the <c>CalendarEvents.Write</c>
/// scope. The request body is empty in this iteration; both ids come from the
/// route. The boundary owns mapping the publish outcome to a status code.
/// </summary>
public class PublishEventPlatformApi(PublishHandler publishHandler)
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
    /// publish body; not-found is 404; past start and missing English title are
    /// 400; already-published, publish-in-progress, and platform-deleted are 409;
    /// an unsupported provider is 501; a provider failure is 502; and a finalize
    /// failure is 500.
    /// </summary>
    public static IActionResult ToResult(
        PublishResult result,
        string calendarEventId,
        string platformId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Outcome switch
        {
            PublishOutcome.Published =>
                new OkObjectResult(ToResponse(result, calendarEventId, platformId)),
            PublishOutcome.EventNotFound =>
                new NotFoundObjectResult($"Calendar event '{calendarEventId}' was not found."),
            PublishOutcome.PlatformNotFound =>
                new NotFoundObjectResult($"Platform '{platformId}' was not found."),
            PublishOutcome.PastStart =>
                new BadRequestObjectResult("The calendar event start must be in the future."),
            PublishOutcome.MissingEnglishTitle =>
                new BadRequestObjectResult("The calendar event requires an English title to publish."),
            PublishOutcome.AlreadyPublished =>
                new ConflictObjectResult(
                    $"Calendar event '{calendarEventId}' is already published to platform '{platformId}'."),
            PublishOutcome.PublishInProgress =>
                new ConflictObjectResult(
                    $"A publish of calendar event '{calendarEventId}' to platform '{platformId}' " +
                    "is already in progress."),
            PublishOutcome.PlatformDeleted =>
                new ConflictObjectResult(
                    $"Platform '{platformId}' was deleted; its publication is read-only history."),
            PublishOutcome.ProviderNotSupported =>
                new ObjectResult($"Publishing to platform '{platformId}' is not supported.")
                {
                    StatusCode = StatusCodes.Status501NotImplemented
                },
            PublishOutcome.ProviderFailed =>
                new ObjectResult(
                    $"Publishing calendar event '{calendarEventId}' to platform '{platformId}' " +
                    "failed at the provider.")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                },
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

    private static PublishEventPlatformResponse ToResponse(
        PublishResult result,
        string calendarEventId,
        string platformId) =>
        new(
            calendarEventId,
            platformId,
            result.PlatformName!,
            result.PlatformType!.Value.ToString(),
            PublishStatus.Published.ToString(),
            result.ExternalResourceId!,
            result.PublishedUtc!.Value);
}
