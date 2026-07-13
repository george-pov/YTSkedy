using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.Scheduling.Application.Settings;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.Settings;

public sealed class StartDefaultsApi(
    GetStartDefaultsHandler getHandler,
    UpdateStartDefaultsHandler updateHandler)
{
    [Function("GetCalendarEventStartDefaults")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "settings/calendar-event-start-defaults")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var defaults = await getHandler.HandleAsync(cancellationToken);
        return new OkObjectResult(ToResponse(defaults));
    }

    [Function("UpdateCalendarEventStartDefaults")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> UpdateAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "settings/calendar-event-start-defaults")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await HttpJsonBody.ReadRequiredAsync<UpdateStartDefaultsRequest>(
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
        UpdateStartDefaultsRequest request,
        out UpdateStartDefaultsCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        DayOfWeek? dayOfWeek = null;
        if (request.DayOfWeek is not null)
        {
            if (!Enum.TryParse<DayOfWeek>(
                    request.DayOfWeek,
                    ignoreCase: false,
                    out var parsedDay) ||
                !Enum.IsDefined(parsedDay))
            {
                error = InvalidDefaultsResult();
                return false;
            }

            dayOfWeek = parsedDay;
        }

        TimeOnly? localTime = null;
        if (request.LocalTime is not null)
        {
            if (!TimeOnly.TryParseExact(
                    request.LocalTime,
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedTime))
            {
                error = InvalidDefaultsResult();
                return false;
            }

            localTime = parsedTime;
        }

        if (request.TimeZoneId is not null &&
            !TimeZoneLookup.TryFind(request.TimeZoneId, out _))
        {
            error = InvalidDefaultsResult();
            return false;
        }

        command = new UpdateStartDefaultsCommand(dayOfWeek, localTime, request.TimeZoneId);
        return true;
    }

    internal static StartDefaultsResponse ToResponse(StartDefaults defaults) =>
        new(
            defaults.DayOfWeek?.ToString(),
            defaults.LocalTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            defaults.TimeZoneId);

    private static IActionResult InvalidDefaultsResult() =>
        new BadRequestObjectResult(
            "dayOfWeek, localTime, and timeZoneId must be null or valid canonical values.");
}
