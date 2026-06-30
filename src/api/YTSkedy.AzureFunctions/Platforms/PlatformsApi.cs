using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// HTTP boundary for configured publishing platforms. Hosts list, get, create,
/// update, and delete under the Azure Functions <c>/api</c> prefix, reusing the
/// calendar-event bearer-token scopes (<c>CalendarEvents.Read</c> for reads,
/// <c>CalendarEvents.Write</c> for writes). The boundary owns request parsing,
/// validation to <c>400 Bad Request</c>, and result-to-status mapping.
/// </summary>
public sealed class PlatformsApi(
    ListPlatformsHandler listHandler,
    GetPlatformHandler getHandler,
    CreatePlatformHandler createHandler,
    UpdatePlatformHandler updateHandler,
    DeletePlatformHandler deleteHandler)
{
    [Function("ListPlatforms")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> ListPlatformsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "platforms")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        PlatformType? typeFilter = null;

        if (!HttpQuery.TryGetSingleValue(request, "type", out var typeValue, out var typeError))
        {
            return typeError;
        }

        if (typeValue is not null)
        {
            if (!TryParsePlatformType(typeValue, out var parsedType))
            {
                return InvalidTypeResult();
            }
            typeFilter = parsedType;
        }

        var views = await listHandler.HandleAsync(
            new ListPlatformsQuery(typeFilter),
            cancellationToken);

        return new OkObjectResult(ToListResponse(views));
    }

    [Function("GetPlatform")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> GetPlatformAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "platforms/{platformId}")]
        HttpRequest request,
        string platformId,
        CancellationToken cancellationToken)
    {
        var view = await getHandler.HandleAsync(platformId, cancellationToken);

        return view is null
            ? new NotFoundResult()
            : new OkObjectResult(ToPlatformResponse(view));
    }

    [Function("CreatePlatform")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> CreatePlatformAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "platforms")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await HttpJsonBody.ReadRequiredAsync<CreatePlatformRequest>(
            request,
            cancellationToken);
        if (body.Error is not null)
        {
            return body.Error;
        }

        var createRequest = body.Value!;
        if (!TryBuildCreateCommand(createRequest, out var command, out var error))
        {
            return error;
        }

        var result = await createHandler.HandleAsync(command, cancellationToken);

        return ToCreateResult(result, command);
    }

    [Function("UpdatePlatform")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> UpdatePlatformAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "platforms/{platformId}")]
        HttpRequest request,
        string platformId,
        CancellationToken cancellationToken)
    {
        var body = await HttpJsonBody.ReadRequiredAsync<UpdatePlatformRequest>(
            request,
            cancellationToken);
        if (body.Error is not null)
        {
            return body.Error;
        }

        var updateRequest = body.Value!;
        var existingPlatform = await getHandler.HandleAsync(platformId, cancellationToken);
        if (existingPlatform is null)
        {
            return new NotFoundObjectResult($"Platform '{platformId}' was not found.");
        }

        if (!TryBuildUpdateCommand(existingPlatform, updateRequest, out var command, out var error))
        {
            return error;
        }

        var result = await updateHandler.HandleAsync(command, cancellationToken);

        return ToUpdateResult(result, command);
    }

    [Function("DeletePlatform")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> DeletePlatformAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "platforms/{platformId}")]
        HttpRequest request,
        string platformId,
        CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(
            new DeletePlatformCommand(platformId),
            cancellationToken);

        return ToDeleteResult(result, platformId);
    }

    /// <summary>
    /// Parses a route or query platform-type segment (case-insensitive) to a
    /// <see cref="PlatformType"/>. Numeric and unknown values are rejected.
    /// </summary>
    internal static bool TryParsePlatformType(string? value, out PlatformType type)
    {
        switch (value?.ToLowerInvariant())
        {
            case "youtube":
                type = PlatformType.YouTube;
                return true;
            case "wordpress":
                type = PlatformType.WordPress;
                return true;
            default:
                type = default;
                return false;
        }
    }

    /// <summary>
    /// Validates a create request at the API boundary and maps it to a command.
    /// The name uses the domain limit, the type is parsed, and the publish
    /// settings are validated against the type. Any failure yields a
    /// <c>400 Bad Request</c> through <paramref name="error"/>.
    /// </summary>
    internal static bool TryBuildCreateCommand(
        CreatePlatformRequest request,
        out CreatePlatformCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        if (!Platform.IsValidName(request.Name))
        {
            error = InvalidNameResult();
            return false;
        }

        if (!TryParsePlatformType(request.Type, out var type))
        {
            error = InvalidTypeResult();
            return false;
        }

        if (!Platform.IsValidReferenceKey(request.ReferenceKey))
        {
            error = InvalidReferenceKeyResult();
            return false;
        }

        if (!PlatformPublishSettingsHttpMapper.TryBuild(
                type,
                request.PublishSettings,
                out var publishSettings,
                out error))
        {
            return false;
        }

        if (!TryBuildPublishingContent(
                request.PublishingContent,
                out var publishingContent,
                out error))
        {
            return false;
        }

        command = new CreatePlatformCommand(
            request.Name!.Trim(),
            type,
            publishSettings,
            publishingContent,
            Platform.NormalizeReferenceKey(request.ReferenceKey));
        return true;
    }

    /// <summary>
    /// Validates an update request and its route id to a command. The type is
    /// immutable and not accepted; the name and publish settings come from the
    /// body. Any failure yields a <c>400 Bad Request</c> through
    /// <paramref name="error"/>.
    /// </summary>
    internal static bool TryBuildUpdateCommand(
        PlatformView existingPlatform,
        UpdatePlatformRequest request,
        out UpdatePlatformCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(existingPlatform);
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        if (!Platform.IsValidName(request.Name))
        {
            error = InvalidNameResult();
            return false;
        }

        if (!Platform.IsValidReferenceKey(request.ReferenceKey))
        {
            error = InvalidReferenceKeyResult();
            return false;
        }

        if (!PlatformPublishSettingsHttpMapper.TryBuild(
                existingPlatform.Type,
                request.PublishSettings,
                existingPlatform.PublishSettings,
                out var publishSettings,
                out error))
        {
            return false;
        }

        if (!TryBuildPublishingContent(
                request.PublishingContent,
                out var publishingContent,
                out error))
        {
            return false;
        }

        command = new UpdatePlatformCommand(
            existingPlatform.PlatformId,
            request.Name!.Trim(),
            Platform.NormalizeReferenceKey(request.ReferenceKey),
            publishSettings,
            publishingContent);
        return true;
    }

    /// <summary>
    /// Maps a create outcome to its HTTP result. Created is 200 with the single
    /// platform shape; duplicate name and duplicate reference key are 409.
    /// </summary>
    internal static IActionResult ToCreateResult(
        CreatePlatformResult result,
        CreatePlatformCommand command) =>
        result.Status switch
        {
            CreatePlatformStatus.Created => new OkObjectResult(
                ToPlatformResponse(
                    result.PlatformId!,
                    command.Name,
                    command.Type,
                    command.ReferenceKey,
                    command.PublishSettings,
                    command.PublishingContent)),
            CreatePlatformStatus.NameAlreadyExists => new ConflictObjectResult(
                $"A platform named '{command.Name}' already exists."),
            CreatePlatformStatus.ReferenceKeyAlreadyExists => new ConflictObjectResult(
                $"A platform reference key '{command.ReferenceKey}' already exists."),
            CreatePlatformStatus.LinkedTemplateNotFound => new BadRequestObjectResult(
                $"One or more publishing content templates were not found for platform type '{command.Type}'."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    /// <summary>
    /// Maps an update outcome to its HTTP result. Updated is 200 with the single
    /// platform shape; an unknown id is 404; duplicate name, duplicate reference
    /// key, and publish-in-progress outcomes are 409.
    /// </summary>
    internal static IActionResult ToUpdateResult(
        UpdatePlatformResult result,
        UpdatePlatformCommand command) =>
        result switch
        {
            UpdatePlatformResult.Updated => new OkObjectResult(
                ToPlatformResponse(
                    command.PlatformId,
                    command.Name,
                    PlatformPublishSettingsHttpMapper.TypeOf(command.PublishSettings),
                    command.ReferenceKey,
                    command.PublishSettings,
                    command.PublishingContent)),
            UpdatePlatformResult.NotFound => new NotFoundObjectResult(
                $"Platform '{command.PlatformId}' was not found."),
            UpdatePlatformResult.NameAlreadyExists => new ConflictObjectResult(
                $"A platform named '{command.Name}' already exists."),
            UpdatePlatformResult.ReferenceKeyAlreadyExists => new ConflictObjectResult(
                $"A platform reference key '{command.ReferenceKey}' already exists."),
            UpdatePlatformResult.LinkedTemplateNotFound => new BadRequestObjectResult(
                "One or more publishing content templates were not found for the platform type."),
            UpdatePlatformResult.Conflict => new ConflictObjectResult(
                $"Platform '{command.PlatformId}' cannot be updated while a publish is in progress."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    /// <summary>
    /// Maps a delete outcome to its HTTP result. Deleted is 204; an unknown id is
    /// 404; a publish in progress is 409.
    /// </summary>
    internal static IActionResult ToDeleteResult(
        DeletePlatformResult result,
        string platformId) =>
        result switch
        {
            DeletePlatformResult.Deleted => new NoContentResult(),
            DeletePlatformResult.NotFound => new NotFoundObjectResult(
                $"Platform '{platformId}' was not found."),
            DeletePlatformResult.Conflict => new ConflictObjectResult(
                $"Platform '{platformId}' cannot be deleted while a publish is in progress."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    internal static PlatformListResponse ToListResponse(IReadOnlyList<PlatformView> views) =>
        new(views.Select(ToPlatformResponse).ToArray());

    internal static PlatformResponse ToPlatformResponse(PlatformView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return ToPlatformResponse(
            view.PlatformId,
            view.Name,
            view.Type,
            view.ReferenceKey,
            view.PublishSettings,
            view.PublishingContent);
    }

    internal static PlatformResponse ToPlatformResponse(
        string platformId,
        string name,
        PlatformType type,
        string? referenceKey,
        PublishSettings publishSettings,
        PublishingContent publishingContent) =>
        new(
            platformId,
            name,
            referenceKey,
            type.ToString(),
            PlatformPublishSettingsHttpMapper.ToResponse(publishSettings),
            ToPlatformPublishingContentResponse(publishingContent));

    internal static PlatformPublishingContentResponse ToPlatformPublishingContentResponse(
        PublishingContent publishingContent) =>
        new(
            publishingContent.TitleTemplateId,
            publishingContent.DescriptionTemplateId);

    private static IActionResult InvalidNameResult() =>
        new BadRequestObjectResult(
            $"Name must be non-empty and at most {Platform.MaxNameLength} characters.");

    private static IActionResult InvalidTypeResult() =>
        new BadRequestObjectResult("Platform type must be 'YouTube' or 'WordPress'.");

    private static IActionResult InvalidReferenceKeyResult() =>
        new BadRequestObjectResult(
            "Reference key must be 1 to 15 characters and contain only letters, numbers, or hyphen.");

    private static IActionResult InvalidPublishingContentResult() =>
        new BadRequestObjectResult(
            "Publishing content titleTemplateId and descriptionTemplateId are required.");

    private static bool TryBuildPublishingContent(
        PublishingContentPayload? payload,
        out PublishingContent publishingContent,
        out IActionResult error)
    {
        publishingContent = default!;
        error = new EmptyResult();

        if (payload is null)
        {
            error = InvalidPublishingContentResult();
            return false;
        }

        try
        {
            publishingContent = new PublishingContent(
                payload.TitleTemplateId!,
                payload.DescriptionTemplateId!);
            return true;
        }
        catch (ArgumentException)
        {
            error = InvalidPublishingContentResult();
            return false;
        }
    }

}
