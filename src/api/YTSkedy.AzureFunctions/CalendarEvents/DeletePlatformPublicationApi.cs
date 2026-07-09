using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.AzureFunctions.CalendarEvents;

/// <summary>
/// HTTP boundary for deleting one platform publication from a calendar event.
/// The route reuses the calendar-event write scope; the global authorization
/// middleware still requires the configured operator app role.
/// </summary>
public sealed class DeletePlatformPublicationApi(DeletePublicationHandler deleteHandler)
{
    [Function("DeletePlatformPublication")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "calendar-events/{calendarEventId}/platforms/{platformId}/publication")]
        HttpRequest request,
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(
            new DeletePublicationCommand(calendarEventId, platformId),
            cancellationToken);

        return ToResult(result, calendarEventId, platformId);
    }

    internal static IActionResult ToResult(
        DeletePublicationResult result,
        string calendarEventId,
        string platformId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            DeletePublicationStatus.Deleted or DeletePublicationStatus.AlreadyNotPublished =>
                new OkObjectResult(CalendarEventsApi.ToEventPlatformResponse(result.Platform!)),
            DeletePublicationStatus.EventNotFound =>
                new NotFoundObjectResult($"Calendar event '{calendarEventId}' was not found."),
            DeletePublicationStatus.PlatformNotFound =>
                new NotFoundObjectResult($"Platform '{platformId}' was not found."),
            DeletePublicationStatus.Orphaned =>
                new ConflictObjectResult(
                    $"Platform '{platformId}' was deleted; its publication is read-only history."),
            DeletePublicationStatus.PastStart =>
                new ConflictObjectResult(
                    $"Calendar event '{calendarEventId}' is not in the future."),
            DeletePublicationStatus.MissingExternalResourceId =>
                new ConflictObjectResult(
                    $"Calendar event '{calendarEventId}' publication for platform " +
                    $"'{platformId}' has no external resource id."),
            DeletePublicationStatus.TargetMismatch =>
                new ConflictObjectResult(
                    $"Platform '{platformId}' no longer matches the publication target."),
            DeletePublicationStatus.PublishInProgress =>
                new ConflictObjectResult(
                    $"A publish of calendar event '{calendarEventId}' to platform '{platformId}' " +
                    "is already in progress."),
            DeletePublicationStatus.ProviderStateConflict =>
                new ConflictObjectResult(
                    $"Provider resource for calendar event '{calendarEventId}' and platform " +
                    $"'{platformId}' cannot be deleted in its current state."),
            DeletePublicationStatus.RowChanged =>
                new ConflictObjectResult(
                    $"Calendar event '{calendarEventId}' publication for platform " +
                    $"'{platformId}' changed before it could be deleted."),
            DeletePublicationStatus.ProviderNotSupported =>
                new ObjectResult($"Deleting publication for platform '{platformId}' is not supported.")
                {
                    StatusCode = StatusCodes.Status501NotImplemented
                },
            DeletePublicationStatus.ProviderFailed =>
                new ObjectResult(
                    $"Deleting provider resource for calendar event '{calendarEventId}' and " +
                    $"platform '{platformId}' failed.")
                {
                    StatusCode = StatusCodes.Status502BadGateway
                },
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }
}
