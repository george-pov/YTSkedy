using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using System.Text.Json;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Platforms;

/// <summary>
/// HTTP boundary for configured publishing platforms. Hosts list, get, create,
/// update, and delete under the Azure Functions <c>/api</c> prefix, reusing the
/// calendar-event bearer-token scopes (<c>CalendarEvents.Read</c> for reads,
/// <c>CalendarEvents.Write</c> for writes). The boundary owns request parsing,
/// validation to <c>400 Bad Request</c>, and result-to-status mapping. The first
/// slice supports YouTube platforms; WordPress is recognized as a type value but
/// cannot be configured yet because no WordPress publish settings are defined.
/// </summary>
public class PlatformsApi(
    ListPlatformsHandler listHandler,
    GetPlatformHandler getHandler,
    CreatePlatformHandler createHandler,
    UpdatePlatformHandler updateHandler,
    DeletePlatformHandler deleteHandler)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function("ListPlatforms")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> ListPlatformsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "platforms")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        PlatformType? typeFilter = null;

        if (request.Query.TryGetValue("type", out var typeValues))
        {
            if (typeValues.Count != 1 ||
                !TryParsePlatformType(typeValues[0], out var parsedType))
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
        CreatePlatformRequest? createRequest;

        try
        {
            createRequest = await JsonSerializer.DeserializeAsync<CreatePlatformRequest>(
                request.Body,
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult("Request body must be valid JSON.");
        }

        if (createRequest is null)
        {
            return new BadRequestObjectResult("Request body is required.");
        }

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
        UpdatePlatformRequest? updateRequest;

        try
        {
            updateRequest = await JsonSerializer.DeserializeAsync<UpdatePlatformRequest>(
                request.Body,
                JsonOptions,
                cancellationToken);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult("Request body must be valid JSON.");
        }

        if (updateRequest is null)
        {
            return new BadRequestObjectResult("Request body is required.");
        }

        if (!TryBuildUpdateCommand(platformId, updateRequest, out var command, out var error))
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
    public static bool TryParsePlatformType(string? value, out PlatformType type)
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
    public static bool TryBuildCreateCommand(
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

        if (type != PlatformType.YouTube)
        {
            error = UnsupportedPlatformResult();
            return false;
        }

        if (!TryBuildPublishSettings(request.PublishSettings, out var publishSettings, out error))
        {
            return false;
        }

        command = new CreatePlatformCommand(request.Name!.Trim(), type, publishSettings);
        return true;
    }

    /// <summary>
    /// Validates an update request and its route id to a command. The type is
    /// immutable and not accepted; the name and publish settings come from the
    /// body. Any failure yields a <c>400 Bad Request</c> through
    /// <paramref name="error"/>.
    /// </summary>
    public static bool TryBuildUpdateCommand(
        string platformId,
        UpdatePlatformRequest request,
        out UpdatePlatformCommand command,
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

        // Only YouTube platforms exist in this slice, so update settings are
        // validated as YouTube publish settings.
        if (!TryBuildPublishSettings(request.PublishSettings, out var publishSettings, out error))
        {
            return false;
        }

        command = new UpdatePlatformCommand(platformId, request.Name!.Trim(), publishSettings);
        return true;
    }

    /// <summary>
    /// Maps a create outcome to its HTTP result. Created is 200 with the single
    /// platform shape; a duplicate name is 409.
    /// </summary>
    public static IActionResult ToCreateResult(
        CreatePlatformResult result,
        CreatePlatformCommand command) =>
        result.Status switch
        {
            CreatePlatformStatus.Created => new OkObjectResult(
                ToPlatformResponse(
                    result.PlatformId!,
                    command.Name,
                    command.Type,
                    command.PublishSettings)),
            CreatePlatformStatus.NameAlreadyExists => new ConflictObjectResult(
                $"A platform named '{command.Name}' already exists."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    /// <summary>
    /// Maps an update outcome to its HTTP result. Updated is 200 with the single
    /// platform shape; an unknown id is 404; a duplicate name is 409; a publish
    /// in progress is 409.
    /// </summary>
    public static IActionResult ToUpdateResult(
        UpdatePlatformResult result,
        UpdatePlatformCommand command) =>
        result switch
        {
            UpdatePlatformResult.Updated => new OkObjectResult(
                ToPlatformResponse(
                    command.PlatformId,
                    command.Name,
                    TypeOf(command.PublishSettings),
                    command.PublishSettings)),
            UpdatePlatformResult.NotFound => new NotFoundObjectResult(
                $"Platform '{command.PlatformId}' was not found."),
            UpdatePlatformResult.NameAlreadyExists => new ConflictObjectResult(
                $"A platform named '{command.Name}' already exists."),
            UpdatePlatformResult.Conflict => new ConflictObjectResult(
                $"Platform '{command.PlatformId}' cannot be updated while a publish is in progress."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    /// <summary>
    /// Maps a delete outcome to its HTTP result. Deleted is 204; an unknown id is
    /// 404; a publish in progress is 409.
    /// </summary>
    public static IActionResult ToDeleteResult(
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

    public static PlatformListResponse ToListResponse(IReadOnlyList<PlatformView> views) =>
        new(views.Select(ToPlatformResponse).ToArray());

    public static PlatformResponse ToPlatformResponse(PlatformView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return ToPlatformResponse(view.PlatformId, view.Name, view.Type, view.PublishSettings);
    }

    private static PlatformResponse ToPlatformResponse(
        string platformId,
        string name,
        PlatformType type,
        PublishSettings publishSettings) =>
        new(
            platformId,
            name,
            type.ToString(),
            ToPublishSettingsResponse(publishSettings));

    private static PublishSettingsResponse ToPublishSettingsResponse(PublishSettings publishSettings) =>
        publishSettings switch
        {
            YouTubePublishSettings youTube => new PublishSettingsResponse(
                youTube.Credentials,
                youTube.PrivacyStatus,
                youTube.SelfDeclaredMadeForKids),
            _ => throw new ArgumentOutOfRangeException(
                nameof(publishSettings),
                publishSettings.GetType().Name,
                "Unknown publish settings type.")
        };

    private static PlatformType TypeOf(PublishSettings publishSettings) =>
        publishSettings switch
        {
            YouTubePublishSettings => PlatformType.YouTube,
            _ => throw new ArgumentOutOfRangeException(
                nameof(publishSettings),
                publishSettings.GetType().Name,
                "Unknown publish settings type.")
        };

    private static bool TryBuildPublishSettings(
        PublishSettingsPayload? payload,
        out PublishSettings publishSettings,
        out IActionResult error)
    {
        publishSettings = default!;
        error = new EmptyResult();

        if (payload is null)
        {
            error = new BadRequestObjectResult("Publish settings are required.");
            return false;
        }

        if (!YouTubePublishSettings.IsValidCredentials(payload.Credentials))
        {
            error = InvalidCredentialsResult();
            return false;
        }

        if (!YouTubePublishSettings.IsValidPrivacyStatus(payload.PrivacyStatus))
        {
            error = InvalidPrivacyStatusResult();
            return false;
        }

        publishSettings = new YouTubePublishSettings(
            payload.Credentials!,
            payload.PrivacyStatus!,
            payload.SelfDeclaredMadeForKids ?? false);
        return true;
    }

    private static IActionResult InvalidNameResult() =>
        new BadRequestObjectResult(
            $"Name must be non-empty and at most {Platform.MaxNameLength} characters.");

    private static IActionResult InvalidTypeResult() =>
        new BadRequestObjectResult("Platform type must be 'YouTube' or 'WordPress'.");

    private static IActionResult UnsupportedPlatformResult() =>
        new BadRequestObjectResult(
            "Only YouTube platforms can be configured in this slice.");

    private static IActionResult InvalidCredentialsResult() =>
        new BadRequestObjectResult(
            "Publish settings credentials must be a non-empty reference name.");

    private static IActionResult InvalidPrivacyStatusResult() =>
        new BadRequestObjectResult(
            "Publish settings privacy status must be 'private', 'public', or 'unlisted'.");
}
