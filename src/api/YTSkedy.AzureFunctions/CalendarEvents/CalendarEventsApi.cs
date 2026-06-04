using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Globalization;
using System.Text.Json;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.CalendarEvents;

public class CalendarEventsApi(
    CreateCalendarEventHandler createHandler,
    ListByMonthHandler listByMonthHandler)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function("CreateCalendarEvent")]
    public async Task<IActionResult> CreateCalendarEventAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "calendar-events")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        CreateCalendarEventRequest? createRequest;

        try
        {
            createRequest = await JsonSerializer.DeserializeAsync<CreateCalendarEventRequest>(
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

        var command = new CreateCalendarEventCommand(
            new ScheduledStart(
                createRequest.Start.LocalDateTime,
                createRequest.Start.TimeZoneId),
            createRequest.Descriptions
                .Select(description => new LocalizedDescription(
                    description.Language,
                    description.Title,
                    description.Description))
                .ToArray());

        var result = await createHandler.HandleAsync(
            command,
            cancellationToken);

        return new OkObjectResult(new CreateCalendarEventResponse(result.CalendarEventId));
    }

    [Function("ListCalendarEventsByMonth")]
    public async Task<IActionResult> ListByMonthAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "calendar-events")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseNumber(request, "year", out var year, out var yearError))
        {
            return yearError;
        }

        if (!ValidateRange("year", year, 1000, 9999, out var yearRangeError))
        {
            return yearRangeError;
        }

        if (!TryParseNumber(request, "month", out var month, out var monthError))
        {
            return monthError;
        }

        if (!ValidateRange("month", month, 1, 12, out var monthRangeError))
        {
            return monthRangeError;
        }

        var criteria = new CalendarEventMonthCriteria(year, month);
        var calendarEvents = await listByMonthHandler.HandleAsync(
            criteria,
            cancellationToken);

        var response = calendarEvents
            .Select(calendarEvent => new CalendarEventListItemResponse(
                calendarEvent.CalendarEventId,
                new CalendarEventStart(
                    calendarEvent.Start.LocalDateTime,
                    calendarEvent.Start.TimeZoneId),
                calendarEvent.Descriptions
                    .Select(description => new LocalizedCalendarEventText(
                        description.Language,
                        description.Title,
                        description.Description))
                    .ToArray()))
            .ToArray();

        return new OkObjectResult(response);
    }

    private static bool TryParseNumber(
        HttpRequest request,
        string name,
        out int value,
        out IActionResult error)
    {
        value = 0;
        error = new EmptyResult();

        if (!request.Query.TryGetValue(name, out var values) || values.Count == 0)
        {
            error = new BadRequestObjectResult($"Query parameter '{name}' is required.");
            return false;
        }

        if (values.Count > 1)
        {
            error = new BadRequestObjectResult(
                $"Query parameter '{name}' must have a single value.");
            return false;
        }

        var rawValue = values[0];
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            error = new BadRequestObjectResult($"Query parameter '{name}' must not be empty.");
            return false;
        }

        if (!int.TryParse(
                rawValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out value))
        {
            error = new BadRequestObjectResult($"Query parameter '{name}' must be an integer.");
            return false;
        }

        return true;
    }

    private static bool ValidateRange(
        string name,
        int value,
        int minValue,
        int maxValue,
        out IActionResult error)
    {
        error = new EmptyResult();

        if (value < minValue || value > maxValue)
        {
            error = new BadRequestObjectResult(
                $"Query parameter '{name}' must be between {minValue} and {maxValue}.");
            return false;
        }

        return true;
    }
}
