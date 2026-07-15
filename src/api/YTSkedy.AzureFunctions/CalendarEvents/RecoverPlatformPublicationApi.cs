using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed class RecoverPlatformPublicationApi(
    RecoverPublicationHandler recoverPublicationHandler)
{
    [Function("RecoverPlatformPublication")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> RecoverAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "post",
            Route = "calendar-events/{calendarEventId}/platforms/{platformId}/publication/recover")]
        HttpRequest request,
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        var result = await recoverPublicationHandler.HandleAsync(
            new RecoverPublicationCommand(calendarEventId, platformId),
            cancellationToken);
        return ToResult(result, calendarEventId, platformId);
    }

    internal static IActionResult ToResult(
        RecoverPublicationResult result,
        string calendarEventId,
        string platformId) =>
        result.Status switch
        {
            RecoverPublicationStatus.Recovered => new NoContentResult(),
            RecoverPublicationStatus.EventNotFound =>
                new NotFoundObjectResult($"Calendar event '{calendarEventId}' was not found."),
            RecoverPublicationStatus.PlatformNotFound =>
                new NotFoundObjectResult($"Platform '{platformId}' was not found."),
            RecoverPublicationStatus.PublicationNotFound =>
                new NotFoundObjectResult(
                    $"Publication for calendar event '{calendarEventId}' and platform " +
                    $"'{platformId}' was not found."),
            RecoverPublicationStatus.PlatformDeleted =>
                Conflict("The platform was deleted. Reload the calendar event details."),
            RecoverPublicationStatus.PastStart =>
                Conflict("The calendar event is no longer in the future. Reload the details."),
            RecoverPublicationStatus.NotPublishing =>
                Conflict("The publication is no longer in progress. Reload the details."),
            RecoverPublicationStatus.NotStale =>
                Conflict("The publication attempt is not stale. Reload the details."),
            RecoverPublicationStatus.RowChanged =>
                Conflict("The publication changed before recovery. Reload the details."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    private static ConflictObjectResult Conflict(string message) => new(message);
}
