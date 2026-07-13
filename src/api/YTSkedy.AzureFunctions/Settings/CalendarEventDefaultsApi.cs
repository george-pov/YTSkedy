using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.Settings;

public sealed class CalendarEventDefaultsApi(
    GetCalendarEventDefaultsHandler getHandler,
    UpdateCalendarEventDefaultsHandler updateHandler)
{
    [Function("GetCalendarEventDefaults")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settings/calendar-event-defaults")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var defaults = await getHandler.HandleAsync(cancellationToken);

        return new OkObjectResult(ToResponse(defaults));
    }

    [Function("UpdateCalendarEventDefaults")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "settings/calendar-event-defaults")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await HttpJsonBody.ReadRequiredAsync<UpdateCalendarEventDefaultsRequest>(
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

        var defaults = await updateHandler.HandleAsync(command, cancellationToken);

        return new OkObjectResult(ToResponse(defaults));
    }

    internal static bool TryBuildUpdateCommand(
        UpdateCalendarEventDefaultsRequest request,
        out UpdateCalendarEventDefaultsCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        if (!TryBuildFields(request.EventTextFields, out var fields))
        {
            error = InvalidFieldsResult();
            return false;
        }

        if (!TryBuildStartDefaults(
                request.StartDefaults,
                out var dayOfWeek,
                out var localTime,
                out var timeZoneId))
        {
            error = InvalidStartDefaultsResult();
            return false;
        }

        command = new UpdateCalendarEventDefaultsCommand(
            fields,
            dayOfWeek,
            localTime,
            timeZoneId);
        return true;
    }

    internal static CalendarEventDefaultsResponse ToResponse(
        CalendarEventDefaults defaults)
    {
        ArgumentNullException.ThrowIfNull(defaults);

        return new CalendarEventDefaultsResponse(
            new EventTextFieldsResponse(
                defaults.EventTextFields.Fields
                    .Select(field => new EventTextFieldResponse(
                        field.FieldKey,
                        field.Label,
                        field.Type.ToString(),
                        field.MaxLength))
                    .ToArray()),
            new StartDefaultsResponse(
                defaults.StartDefaults.DayOfWeek?.ToString(),
                defaults.StartDefaults.LocalTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
                defaults.StartDefaults.TimeZoneId));
    }

    private static bool TryBuildFields(
        UpdateEventTextFieldsRequest? request,
        out IReadOnlyCollection<EventTextField> fields)
    {
        fields = [];
        if (request?.Fields is null || request.Fields.Count == 0)
        {
            return false;
        }

        var parsedFields = new List<EventTextField>();
        foreach (var field in request.Fields)
        {
            if (field is null ||
                !EventTextField.IsValidLabel(field.Label) ||
                !EventTextField.IsValidMaxLength(field.MaxLength) ||
                !EventTextTypeParser.TryParse(field.Type, out var type))
            {
                return false;
            }

            parsedFields.Add(new EventTextField(
                field.Label!,
                type,
                field.MaxLength));
        }

        fields = parsedFields;
        return true;
    }

    private static bool TryBuildStartDefaults(
        UpdateStartDefaultsRequest? request,
        out DayOfWeek? dayOfWeek,
        out TimeOnly? localTime,
        out string? timeZoneId)
    {
        dayOfWeek = null;
        localTime = null;
        timeZoneId = null;
        if (request is null)
        {
            return false;
        }

        if (request.DayOfWeek is not null)
        {
            if (!Enum.TryParse<DayOfWeek>(
                    request.DayOfWeek,
                    ignoreCase: false,
                    out var parsedDay) ||
                !Enum.IsDefined(parsedDay))
            {
                return false;
            }

            dayOfWeek = parsedDay;
        }

        if (request.LocalTime is not null)
        {
            if (!TimeOnly.TryParseExact(
                    request.LocalTime,
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTime))
            {
                return false;
            }

            localTime = parsedTime;
        }

        if (request.TimeZoneId is not null &&
            !TimeZoneLookup.TryFind(request.TimeZoneId, out _))
        {
            return false;
        }

        timeZoneId = request.TimeZoneId;
        return true;
    }

    private static IActionResult InvalidFieldsResult() =>
        new BadRequestObjectResult(
            "eventTextFields.fields must contain at least one valid field with label, type, and maxLength.");

    private static IActionResult InvalidStartDefaultsResult() =>
        new BadRequestObjectResult(
            "startDefaults must contain null or valid canonical dayOfWeek, localTime, and timeZoneId values.");
}
