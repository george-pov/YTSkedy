using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using System.Text.Json;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.CalendarEvents;

public class CalendarEventsApi(CreateCalendarEventHandler handler)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function("CreateCalendarEvent")]
    public async Task<IActionResult> CreateCalendarEventAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "calendar-events")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        CreateEventRequest? createRequest;

        try
        {
            createRequest = await JsonSerializer.DeserializeAsync<CreateEventRequest>(
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

        var result = await handler.HandleAsync(
            command,
            cancellationToken);

        return new OkObjectResult(new CreateEventResponse(result.EventId));
    }
}
