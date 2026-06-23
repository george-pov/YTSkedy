using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using System.Text.Json;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.AzureFunctions.Templates;

public sealed class TemplatesApi(
    ListTemplatesHandler listHandler,
    CreateTemplateHandler createHandler,
    UpdateTemplateHandler updateHandler,
    DeleteTemplateHandler deleteHandler,
    ListTemplateTokensHandler tokensHandler)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function("ListTemplates")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> ListTemplatesAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "templates")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        TemplateType? typeFilter = null;

        if (request.Query.TryGetValue("type", out var typeValues))
        {
            if (typeValues.Count != 1 ||
                !TryParseTemplateType(typeValues[0], out var parsedType))
            {
                return InvalidTypeResult();
            }

            typeFilter = parsedType;
        }

        var views = await listHandler.HandleAsync(
            new ListTemplatesQuery(typeFilter),
            cancellationToken);

        return new OkObjectResult(ToListResponse(views));
    }

    [Function("CreateTemplate")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> CreateTemplateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "templates")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        CreateTemplateRequest? createRequest;

        try
        {
            createRequest = await JsonSerializer.DeserializeAsync<CreateTemplateRequest>(
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

        return ToCreateResult(result, command.Name, command.Type.ToString());
    }

    [Function("UpdateTemplate")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> UpdateTemplateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "templates/{type}/{id}")]
        HttpRequest request,
        string type,
        string id,
        CancellationToken cancellationToken)
    {
        UpdateTemplateRequest? updateRequest;

        try
        {
            updateRequest = await JsonSerializer.DeserializeAsync<UpdateTemplateRequest>(
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

        if (!TryBuildUpdateCommand(type, id, updateRequest, out var command, out var error))
        {
            return error;
        }

        var result = await updateHandler.HandleAsync(command, cancellationToken);

        return ToUpdateResult(result, command.Id, command.Name, command.Type.ToString());
    }

    [Function("DeleteTemplate")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> DeleteTemplateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "templates/{type}/{id}")]
        HttpRequest request,
        string type,
        string id,
        CancellationToken cancellationToken)
    {
        if (!TryParseTemplateType(type, out var parsedType))
        {
            return InvalidTypeResult();
        }

        var result = await deleteHandler.HandleAsync(
            new DeleteTemplateCommand(parsedType, id),
            cancellationToken);

        return ToDeleteResult(result, id);
    }

    [Function("ListTemplateTokens")]
    [RequiredScope("CalendarEvents.Read")]
    public Task<IActionResult> ListTemplateTokensAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "template-tokens")]
        HttpRequest request)
    {
        var tokens = tokensHandler.Handle();

        return Task.FromResult<IActionResult>(
            new OkObjectResult(ToTokenListResponse(tokens)));
    }

    /// <summary>
    /// Parses a route or query template-type segment (case-insensitive) to a
    /// <see cref="TemplateType"/>. Numeric and unknown values are rejected so an
    /// out-of-range enum can never reach the partition-key scheme.
    /// </summary>
    internal static bool TryParseTemplateType(string? value, out TemplateType type)
    {
        switch (value?.ToLowerInvariant())
        {
            case "youtube":
                type = TemplateType.YouTube;
                return true;
            case "wordpress":
                type = TemplateType.WordPress;
                return true;
            default:
                type = default;
                return false;
        }
    }

    /// <summary>
    /// Validates a create request at the API boundary and maps it to a command.
    /// Name and content length use the domain limits, and the type is parsed; any
    /// failure yields a <c>400 Bad Request</c> through <paramref name="error"/>.
    /// </summary>
    internal static bool TryBuildCreateCommand(
        CreateTemplateRequest request,
        out CreateTemplateCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        if (!Template.IsValidName(request.Name))
        {
            error = InvalidNameResult();
            return false;
        }

        if (!Template.IsValidContent(request.Content))
        {
            error = InvalidContentResult();
            return false;
        }

        if (!TryParseTemplateType(request.Type, out var type))
        {
            error = InvalidTypeResult();
            return false;
        }

        command = new CreateTemplateCommand(request.Name, type, request.Content);
        return true;
    }

    /// <summary>
    /// Validates an update request and its route type to a command. The type
    /// comes from the route; name and content come from the body. Any failure
    /// yields a <c>400 Bad Request</c> through <paramref name="error"/>.
    /// </summary>
    internal static bool TryBuildUpdateCommand(
        string type,
        string id,
        UpdateTemplateRequest request,
        out UpdateTemplateCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        if (!TryParseTemplateType(type, out var parsedType))
        {
            error = InvalidTypeResult();
            return false;
        }

        if (!Template.IsValidName(request.Name))
        {
            error = InvalidNameResult();
            return false;
        }

        if (!Template.IsValidContent(request.Content))
        {
            error = InvalidContentResult();
            return false;
        }

        command = new UpdateTemplateCommand(parsedType, id, request.Name, request.Content);
        return true;
    }

    /// <summary>
    /// Maps a create outcome to its HTTP result. Created is 200 with the new id;
    /// a duplicate name within the type is 409.
    /// </summary>
    internal static IActionResult ToCreateResult(
        CreateTemplateResult result,
        string name,
        string type) =>
        result.Status switch
        {
            CreateTemplateStatus.Created => new OkObjectResult(
                new CreateTemplateResponse(result.TemplateId!, name, type)),
            CreateTemplateStatus.NameAlreadyExists => new ConflictObjectResult(
                $"A {type} template named '{name}' already exists."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    /// <summary>
    /// Maps an update outcome to its HTTP result. Updated is 200; an unknown id
    /// is 404; a duplicate name within the type is 409.
    /// </summary>
    internal static IActionResult ToUpdateResult(
        UpdateTemplateResult result,
        string id,
        string name,
        string type) =>
        result switch
        {
            UpdateTemplateResult.Updated => new OkObjectResult(
                new UpdateTemplateResponse(id, name, type)),
            UpdateTemplateResult.NotFound => new NotFoundObjectResult(
                $"Template '{id}' was not found."),
            UpdateTemplateResult.NameAlreadyExists => new ConflictObjectResult(
                $"A {type} template named '{name}' already exists."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    /// <summary>
    /// Maps a delete outcome to its HTTP result. Deleted is 204; an unknown id is
    /// 404.
    /// </summary>
    internal static IActionResult ToDeleteResult(
        DeleteTemplateResult result,
        string id) =>
        result switch
        {
            DeleteTemplateResult.Deleted => new NoContentResult(),
            DeleteTemplateResult.NotFound => new NotFoundObjectResult(
                $"Template '{id}' was not found."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    internal static TemplateListResponse ToListResponse(IReadOnlyList<TemplateView> views) =>
        new(views
            .Select(view => new TemplateResponse(
                view.Id,
                view.Name,
                view.Type.ToString(),
                view.Content))
            .ToArray());

    internal static TemplateTokenListResponse ToTokenListResponse(
        IReadOnlyList<TemplateToken> tokens) =>
        new(tokens
            .Select(token => new TemplateTokenResponse(token.Name))
            .ToArray());

    private static IActionResult InvalidNameResult() =>
        new BadRequestObjectResult(
            $"Name must be non-empty and at most {Template.MaxNameLength} characters.");

    private static IActionResult InvalidContentResult() =>
        new BadRequestObjectResult(
            $"Content must be non-empty and at most {Template.MaxContentLength} characters.");

    private static IActionResult InvalidTypeResult() =>
        new BadRequestObjectResult("Template type must be 'YouTube' or 'WordPress'.");
}
