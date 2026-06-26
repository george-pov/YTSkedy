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
/// validation to <c>400 Bad Request</c>, and result-to-status mapping.
/// </summary>
public sealed class PlatformsApi(
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

        if (!TryBuildPublishSettings(type, request.PublishSettings, out var publishSettings, out error))
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

        if (!TryBuildPublishSettings(
                existingPlatform.Type,
                request.PublishSettings,
                existingPlatform.PublishSettings,
                out var publishSettings,
                out error))
        {
            return false;
        }

        command = new UpdatePlatformCommand(
            existingPlatform.PlatformId,
            request.Name!.Trim(),
            publishSettings);
        return true;
    }

    /// <summary>
    /// Maps a create outcome to its HTTP result. Created is 200 with the single
    /// platform shape; a duplicate name is 409.
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
    internal static IActionResult ToUpdateResult(
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

        return ToPlatformResponse(view.PlatformId, view.Name, view.Type, view.PublishSettings);
    }

    internal static PlatformResponse ToPlatformResponse(
        string platformId,
        string name,
        PlatformType type,
        PublishSettings publishSettings) =>
        new(
            platformId,
            name,
            type.ToString(),
            ToPublishSettingsResponse(publishSettings));

    internal static PublishSettingsResponse ToPublishSettingsResponse(PublishSettings publishSettings) =>
        publishSettings switch
        {
            YouTubeSettings youTube => new PublishSettingsResponse(
                youTube.Credentials,
                youTube.PrivacyStatus,
                youTube.SelfDeclaredMadeForKids,
                null,
                null,
                null,
                null),
            WordPressSettings wordPress => new PublishSettingsResponse(
                null,
                null,
                null,
                wordPress.SiteUrl,
                wordPress.Username,
                wordPress.PostStatus,
                WordPressSettings.IsValidApplicationPassword(wordPress.ApplicationPassword)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(publishSettings),
                publishSettings.GetType().Name,
                "Unknown publish settings type.")
        };

    internal static PlatformType TypeOf(PublishSettings publishSettings) =>
        publishSettings switch
        {
            YouTubeSettings => PlatformType.YouTube,
            WordPressSettings => PlatformType.WordPress,
            _ => throw new ArgumentOutOfRangeException(
                nameof(publishSettings),
                publishSettings.GetType().Name,
                "Unknown publish settings type.")
        };

    private static bool TryBuildPublishSettings(
        PlatformType type,
        PublishSettingsPayload? payload,
        out PublishSettings publishSettings,
        out IActionResult error) =>
        TryBuildPublishSettings(type, payload, currentSettings: null, out publishSettings, out error);

    private static bool TryBuildPublishSettings(
        PlatformType type,
        PublishSettingsPayload? payload,
        PublishSettings? currentSettings,
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

        return type switch
        {
            PlatformType.YouTube => TryBuildYouTubeSettings(payload, out publishSettings, out error),
            PlatformType.WordPress => TryBuildWordPressSettings(
                payload,
                currentSettings as WordPressSettings,
                out publishSettings,
                out error),
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Unknown platform type.")
        };
    }

    private static bool TryBuildYouTubeSettings(
        PublishSettingsPayload payload,
        out PublishSettings publishSettings,
        out IActionResult error)
    {
        publishSettings = default!;
        error = new EmptyResult();

        if (!YouTubeSettings.IsValidCredentials(payload.Credentials))
        {
            error = InvalidCredentialsResult();
            return false;
        }

        if (!YouTubeSettings.IsValidPrivacyStatus(payload.PrivacyStatus))
        {
            error = InvalidPrivacyStatusResult();
            return false;
        }

        publishSettings = new YouTubeSettings(
            payload.Credentials!,
            payload.PrivacyStatus!,
            payload.SelfDeclaredMadeForKids ?? false);
        return true;
    }

    private static bool TryBuildWordPressSettings(
        PublishSettingsPayload payload,
        WordPressSettings? currentSettings,
        out PublishSettings publishSettings,
        out IActionResult error)
    {
        publishSettings = default!;
        error = new EmptyResult();

        if (!TryValidateWordPressSiteUrl(payload.SiteUrl, out error))
        {
            return false;
        }

        if (!WordPressSettings.IsValidUsername(payload.Username))
        {
            error = InvalidWordPressUsernameResult();
            return false;
        }

        var applicationPassword = payload.ApplicationPassword;
        if (string.IsNullOrWhiteSpace(applicationPassword))
        {
            if (currentSettings is null)
            {
                error = MissingWordPressApplicationPasswordResult();
                return false;
            }

            applicationPassword = currentSettings.ApplicationPassword;
        }

        if (!WordPressSettings.IsValidPostStatus(payload.PostStatus))
        {
            error = InvalidWordPressPostStatusResult();
            return false;
        }

        publishSettings = new WordPressSettings(
            payload.SiteUrl!,
            payload.Username!,
            applicationPassword,
            payload.PostStatus!);
        return true;
    }

    private static bool TryValidateWordPressSiteUrl(string? siteUrl, out IActionResult error)
    {
        error = new EmptyResult();

        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            error = MissingWordPressSiteUrlResult();
            return false;
        }

        if (!Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme is not "http" and not "https" ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            error = InvalidWordPressSiteUrlResult();
            return false;
        }

        if (uri.Scheme == "http" &&
            !string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
            uri.Host != "127.0.0.1")
        {
            error = InsecureWordPressSiteUrlResult();
            return false;
        }

        return true;
    }

    private static IActionResult InvalidNameResult() =>
        new BadRequestObjectResult(
            $"Name must be non-empty and at most {Platform.MaxNameLength} characters.");

    private static IActionResult InvalidTypeResult() =>
        new BadRequestObjectResult("Platform type must be 'YouTube' or 'WordPress'.");

    private static IActionResult InvalidCredentialsResult() =>
        new BadRequestObjectResult(
            "Publish settings credentials must be a non-empty reference name.");

    private static IActionResult InvalidPrivacyStatusResult() =>
        new BadRequestObjectResult(
            "Publish settings privacy status must be 'private', 'public', or 'unlisted'.");

    private static IActionResult MissingWordPressSiteUrlResult() =>
        new BadRequestObjectResult("Publish settings site URL is required.");

    private static IActionResult InvalidWordPressSiteUrlResult() =>
        new BadRequestObjectResult(
            "Publish settings site URL must be an absolute HTTP(S) URL without credentials.");

    private static IActionResult InsecureWordPressSiteUrlResult() =>
        new BadRequestObjectResult(
            "Publish settings site URL must use HTTPS unless it targets localhost or 127.0.0.1.");

    private static IActionResult InvalidWordPressUsernameResult() =>
        new BadRequestObjectResult("Publish settings username is required.");

    private static IActionResult MissingWordPressApplicationPasswordResult() =>
        new BadRequestObjectResult("Publish settings Application Password is required.");

    private static IActionResult InvalidWordPressPostStatusResult() =>
        new BadRequestObjectResult("Publish settings post status must be 'publish' or 'draft'.");
}
