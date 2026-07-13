using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.Scheduling.Application.CalendarEvents.Starts;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed class DefaultStartApi(GetDefaultStartHandler handler)
{
    [Function("GetCalendarEventDefaultStart")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar-events/start-suggestion")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetFallbackTimeZoneId(request, out var fallbackTimeZoneId, out var error))
        {
            return error;
        }

        var defaultStart = await handler.HandleAsync(fallbackTimeZoneId, cancellationToken);
        return new OkObjectResult(ToResponse(defaultStart));
    }

    internal static bool TryGetFallbackTimeZoneId(
        HttpRequest request,
        out string? fallbackTimeZoneId,
        out IActionResult error)
    {
        if (!HttpQuery.TryGetSingleValue(
                request,
                "fallbackTimeZoneId",
                out fallbackTimeZoneId,
                out error))
        {
            return false;
        }

        if (fallbackTimeZoneId is not null &&
            !TimeZoneLookup.TryFind(fallbackTimeZoneId, out _))
        {
            error = InvalidFallbackTimeZoneResult();
            return false;
        }

        return true;
    }

    internal static DefaultStartResponse ToResponse(DefaultStart defaultStart) =>
        new(
            defaultStart.LocalDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            defaultStart.LocalTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            defaultStart.TimeZoneId);

    private static IActionResult InvalidFallbackTimeZoneResult() =>
        new BadRequestObjectResult("fallbackTimeZoneId must be a recognized IANA time zone id.");
}
