using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.Settings;

public sealed class EventTextFieldsApi(
    GetEventTextFieldsHandler getHandler,
    UpdateEventTextFieldsHandler updateHandler)
{
    [Function("GetEventTextFields")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settings/event-text-fields")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var eventTextFields = await getHandler.HandleAsync(cancellationToken);

        return new OkObjectResult(ToResponse(eventTextFields));
    }

    [Function("UpdateEventTextFields")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "settings/event-text-fields")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await HttpJsonBody.ReadRequiredAsync<UpdateEventTextFieldsRequest>(
            request,
            cancellationToken);
        if (body.Error is not null)
        {
            return body.Error;
        }

        if (!TryBuildUpdateCommand(body.Value!, out var command, out var error))
        {
            return error;
        }

        var eventTextFields = await updateHandler.HandleAsync(command, cancellationToken);

        return new OkObjectResult(ToResponse(eventTextFields));
    }

    internal static bool TryBuildUpdateCommand(
        UpdateEventTextFieldsRequest request,
        out UpdateEventTextFieldsCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        if (request.Fields is null || request.Fields.Count == 0)
        {
            error = InvalidFieldsResult();
            return false;
        }

        var fields = new List<EventTextField>();

        foreach (var field in request.Fields)
        {
            if (field is null ||
                !EventTextField.IsValidLabel(field.Label) ||
                !EventTextField.IsValidMaxLength(field.MaxLength) ||
                !TryParseEventTextType(field.Type, out var type))
            {
                error = InvalidFieldsResult();
                return false;
            }

            fields.Add(new EventTextField(
                field.FieldKey ?? string.Empty,
                field.Label!,
                type,
                field.MaxLength));
        }

        command = new UpdateEventTextFieldsCommand(fields);
        return true;
    }

    internal static EventTextFieldsResponse ToResponse(EventTextFields eventTextFields)
    {
        ArgumentNullException.ThrowIfNull(eventTextFields);

        return new EventTextFieldsResponse(
            eventTextFields.Fields
                .Select(field => new EventTextFieldResponse(
                    field.FieldKey,
                    field.Label,
                    field.Type.ToString(),
                    field.MaxLength))
                .ToArray());
    }

    private static bool TryParseEventTextType(string? value, out EventTextType type)
    {
        switch (value?.ToLowerInvariant())
        {
            case "shorttext":
                type = EventTextType.ShortText;
                return true;
            case "longtext":
                type = EventTextType.LongText;
                return true;
            default:
                type = default;
                return false;
        }
    }

    private static IActionResult InvalidFieldsResult() =>
        new BadRequestObjectResult(
            "Fields must contain at least one valid field with label, type, and maxLength.");
}
