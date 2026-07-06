using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed class CalendarEventThumbnailsApi(
    UploadThumbnailHandler uploadHandler,
    GetThumbnailHandler getHandler,
    DeleteThumbnailHandler deleteHandler)
{
    [Function("UploadCalendarEventThumbnail")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> UploadAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "put",
            Route = "calendar-events/{calendarEventId}/thumbnail")]
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var buildResult = await BuildUploadCommandAsync(
                request,
                calendarEventId,
                cancellationToken);
        if (buildResult.Error is not null)
        {
            return buildResult.Error;
        }

        var result = await uploadHandler.HandleAsync(buildResult.Command!, cancellationToken);

        return ToUploadResult(result, calendarEventId);
    }

    [Function("GetCalendarEventThumbnail")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "calendar-events/{calendarEventId}/thumbnail")]
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(calendarEventId, cancellationToken);

        return ToGetResult(result, calendarEventId);
    }

    [Function("DeleteCalendarEventThumbnail")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "calendar-events/{calendarEventId}/thumbnail")]
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(calendarEventId, cancellationToken);

        return ToDeleteResult(result, calendarEventId);
    }

    internal static IActionResult ToUploadResult(
        UploadThumbnailResult result,
        string calendarEventId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            UploadThumbnailStatus.Uploaded => new OkObjectResult(
                ToThumbnailResponse(result.Thumbnail!)),
            UploadThumbnailStatus.EventNotFound => new NotFoundObjectResult(
                $"Calendar event '{calendarEventId}' was not found."),
            UploadThumbnailStatus.HasPlatformPublications => new ConflictObjectResult(
                $"Calendar event '{calendarEventId}' has platform publications. " +
                "Delete platform publications before replacing the thumbnail."),
            UploadThumbnailStatus.Invalid => new BadRequestObjectResult(
                DescribeValidationError(result.ValidationError!.Value)),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

    internal static IActionResult ToGetResult(
        GetThumbnailResult result,
        string calendarEventId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            GetThumbnailStatus.Found => new FileContentResult(
                result.Content!.Content,
                result.Content.ContentType),
            GetThumbnailStatus.EventNotFound => new NotFoundObjectResult(
                $"Calendar event '{calendarEventId}' was not found."),
            GetThumbnailStatus.ThumbnailNotFound => new NotFoundObjectResult(
                $"Calendar event '{calendarEventId}' does not have a thumbnail."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

    internal static IActionResult ToDeleteResult(
        DeleteThumbnailResult result,
        string calendarEventId)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            DeleteThumbnailStatus.Deleted => new NoContentResult(),
            DeleteThumbnailStatus.EventNotFound => new NotFoundObjectResult(
                $"Calendar event '{calendarEventId}' was not found."),
            DeleteThumbnailStatus.ThumbnailNotFound => new NotFoundObjectResult(
                $"Calendar event '{calendarEventId}' does not have a thumbnail."),
            DeleteThumbnailStatus.HasPlatformPublications => new ConflictObjectResult(
                $"Calendar event '{calendarEventId}' has platform publications. " +
                "Delete platform publications before deleting the thumbnail."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

    internal static ThumbnailResponse ToThumbnailResponse(Thumbnail thumbnail)
    {
        ArgumentNullException.ThrowIfNull(thumbnail);

        return new ThumbnailResponse(
            thumbnail.FileName,
            thumbnail.ContentType,
            thumbnail.SizeBytes,
            thumbnail.Width,
            thumbnail.Height,
            thumbnail.UpdatedUtc);
    }

    private static async Task<UploadCommandBuildResult> BuildUploadCommandAsync(
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return UploadCommandBuildResult.Failure(new BadRequestObjectResult(
                "Thumbnail upload must use multipart/form-data."));
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return UploadCommandBuildResult.Failure(new BadRequestObjectResult(
                "Thumbnail upload form-data could not be read."));
        }

        var file = form.Files.GetFile("thumbnail");
        if (file is null || file.Length == 0)
        {
            return UploadCommandBuildResult.Failure(new BadRequestObjectResult(
                "Multipart form-data file part 'thumbnail' is required."));
        }

        await using var stream = file.OpenReadStream();
        using var content = new MemoryStream();
        await stream.CopyToAsync(content, cancellationToken);

        return UploadCommandBuildResult.Success(new UploadThumbnailCommand(
            calendarEventId,
            file.FileName,
            file.ContentType,
            content.ToArray()));
    }

    private static string DescribeValidationError(ThumbnailValidationError error) =>
        error switch
        {
            ThumbnailValidationError.UnsupportedExtension =>
                "Thumbnail file name must end with .jpg, .jpeg, or .png.",
            ThumbnailValidationError.UnsupportedContentType =>
                "Thumbnail content type must be image/jpeg or image/png.",
            ThumbnailValidationError.TooLarge =>
                "Thumbnail file size must be 2 MB or smaller.",
            ThumbnailValidationError.UnreadableImage =>
                "Thumbnail image dimensions could not be read.",
            _ => "Thumbnail upload is invalid."
        };

    private sealed record UploadCommandBuildResult(
        UploadThumbnailCommand? Command,
        IActionResult? Error)
    {
        public static UploadCommandBuildResult Success(UploadThumbnailCommand command) =>
            new(command, null);

        public static UploadCommandBuildResult Failure(IActionResult error) =>
            new(null, error);
    }
}
