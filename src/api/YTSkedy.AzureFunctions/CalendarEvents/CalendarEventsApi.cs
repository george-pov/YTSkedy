using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using System.Globalization;
using System.Text.Json;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed class CalendarEventsApi(
    CreateCalendarEventHandler createHandler,
    ListEventsHandler listHandler,
    GetCalendarEventDetailHandler getDetailHandler,
    UpdateCalendarEventHandler updateHandler,
    DeleteCalendarEventHandler deleteHandler)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function("CreateCalendarEvent")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> CreateCalendarEventAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calendar-events")]
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

    [Function("ListCalendarEvents")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> ListAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar-events")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParsePaging(request, out var page, out var pageSize, out var pagingError))
        {
            return pagingError;
        }

        if (!TryParseSort(request, out var sort, out var direction, out var sortError))
        {
            return sortError;
        }

        if (!TryParseOptionalMonth(request, out var year, out var month, out var monthError))
        {
            return monthError;
        }

        var query = new CalendarEventListQuery(page, pageSize, sort, direction, year, month);
        var result = await listHandler.HandleAsync(query, cancellationToken);

        var response = new CalendarEventListResponse(
            result.Items.Select(ToViewResponse).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            ToSortString(result.Sort),
            ToDirectionString(result.Direction));

        return new OkObjectResult(response);
    }

    [Function("GetCalendarEvent")]
    [RequiredScope("CalendarEvents.Read")]
    public async Task<IActionResult> GetCalendarEventAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "calendar-events/{calendarEventId}")]
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var detail = await getDetailHandler.HandleAsync(calendarEventId, cancellationToken);

        return detail is null
            ? new NotFoundResult()
            : new OkObjectResult(ToDetailResponse(detail));
    }

    [Function("UpdateCalendarEvent")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> UpdateCalendarEventAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "calendar-events/{calendarEventId}")]
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        UpdateCalendarEventRequest? updateRequest;

        try
        {
            updateRequest = await JsonSerializer.DeserializeAsync<UpdateCalendarEventRequest>(
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

        var command = new UpdateDescriptionsCommand(
            calendarEventId,
            updateRequest.Descriptions
                .Select(description => new LocalizedDescription(
                    description.Language,
                    description.Title,
                    description.Description))
                .ToArray());

        var result = await updateHandler.HandleAsync(command, cancellationToken);

        return result switch
        {
            UpdateCalendarEventResult.Updated => new OkObjectResult(
                new UpdateCalendarEventResponse(calendarEventId)),
            UpdateCalendarEventResult.NotFound => new NotFoundResult(),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };
    }

    [Function("DeleteCalendarEvent")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> DeleteCalendarEventAsync(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "delete",
            Route = "calendar-events/{calendarEventId}")]
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(
            calendarEventId,
            cancellationToken);

        return ToDeleteResult(result, calendarEventId);
    }

    internal static IActionResult ToDeleteResult(
        DeleteCalendarEventResult result,
        string calendarEventId) =>
        result switch
        {
            DeleteCalendarEventResult.Deleted => new NoContentResult(),
            DeleteCalendarEventResult.NotFound => new NotFoundObjectResult(
                $"Calendar event '{calendarEventId}' was not found."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    private static bool TryParsePaging(
        HttpRequest request,
        out int page,
        out int pageSize,
        out IActionResult error)
    {
        page = 0;
        pageSize = 10;

        if (!TryGetSingleValue(request, "page", out var pageValue, out error))
        {
            return false;
        }

        if (pageValue is not null &&
            !int.TryParse(pageValue, NumberStyles.None, CultureInfo.InvariantCulture, out page))
        {
            error = new BadRequestObjectResult(
                "Query parameter 'page' must be a non-negative integer.");
            return false;
        }

        if (!TryGetSingleValue(request, "pageSize", out var pageSizeValue, out error))
        {
            return false;
        }

        if (pageSizeValue is not null &&
            (!int.TryParse(
                pageSizeValue,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out pageSize) ||
            pageSize < 1 ||
            pageSize > 100))
        {
            error = new BadRequestObjectResult(
                "Query parameter 'pageSize' must be an integer between 1 and 100.");
            return false;
        }

        error = new EmptyResult();
        return true;
    }

    private static bool TryParseSort(
        HttpRequest request,
        out CalendarEventSortField sort,
        out SortDirection direction,
        out IActionResult error)
    {
        sort = CalendarEventSortField.ScheduledStart;
        direction = SortDirection.Descending;

        if (!TryGetSingleValue(request, "sort", out var sortValue, out error))
        {
            return false;
        }

        if (sortValue is not null)
        {
            switch (sortValue.ToLowerInvariant())
            {
                case "scheduledstart":
                    sort = CalendarEventSortField.ScheduledStart;
                    break;
                case "timezone":
                    sort = CalendarEventSortField.TimeZone;
                    break;
                case "title":
                    sort = CalendarEventSortField.Title;
                    break;
                default:
                    error = new BadRequestObjectResult(
                        "Query parameter 'sort' must be 'scheduledStart', 'timeZone', or 'title'.");
                    return false;
            }
        }

        if (!TryGetSingleValue(request, "direction", out var directionValue, out error))
        {
            return false;
        }

        if (directionValue is not null)
        {
            switch (directionValue.ToLowerInvariant())
            {
                case "asc":
                    direction = SortDirection.Ascending;
                    break;
                case "desc":
                    direction = SortDirection.Descending;
                    break;
                default:
                    error = new BadRequestObjectResult(
                        "Query parameter 'direction' must be 'asc' or 'desc'.");
                    return false;
            }
        }

        error = new EmptyResult();
        return true;
    }

    private static bool TryParseOptionalMonth(
        HttpRequest request,
        out int? year,
        out int? month,
        out IActionResult error)
    {
        year = null;
        month = null;
        error = new EmptyResult();

        var hasYear = request.Query.ContainsKey("year");
        var hasMonth = request.Query.ContainsKey("month");

        if (!hasYear && !hasMonth)
        {
            return true;
        }

        if (hasYear != hasMonth)
        {
            error = new BadRequestObjectResult(
                "Query parameters 'year' and 'month' must be provided together.");
            return false;
        }

        if (!TryParseNumber(request, "year", out var parsedYear, out error))
        {
            return false;
        }

        if (!ValidateRange("year", parsedYear, 1000, 9999, out error))
        {
            return false;
        }

        if (!TryParseNumber(request, "month", out var parsedMonth, out error))
        {
            return false;
        }

        if (!ValidateRange("month", parsedMonth, 1, 12, out error))
        {
            return false;
        }

        year = parsedYear;
        month = parsedMonth;
        return true;
    }

    private static bool TryGetSingleValue(
        HttpRequest request,
        string name,
        out string? value,
        out IActionResult error)
    {
        value = null;
        error = new EmptyResult();

        if (!request.Query.TryGetValue(name, out var values) || values.Count == 0)
        {
            return true;
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

        value = rawValue;
        return true;
    }

    private static CalendarEventViewResponse ToViewResponse(
        CalendarEventView calendarEvent)
    {
        return new(
            calendarEvent.CalendarEventId,
            new CalendarEventStart(
                calendarEvent.Start.LocalDateTime,
                calendarEvent.Start.TimeZoneId),
            calendarEvent.ScheduledStartUtc,
            calendarEvent.Descriptions
                .Select(description => new LocalizedCalendarEventText(
                    description.Language,
                    description.Title,
                    description.Description))
                .ToArray());
    }

    /// <summary>
    /// Maps the calendar event detail read model to the get-by-id response. The
    /// event fields mirror one list item; <c>platforms</c> is mapped by
    /// <see cref="ToEventPlatformResponse"/>. This detail response is the only
    /// place the per-platform publication state is exposed over HTTP.
    /// </summary>
    internal static CalendarEventDetailResponse ToDetailResponse(CalendarEventDetailView detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        var calendarEvent = detail.Event;

        return new CalendarEventDetailResponse(
            calendarEvent.CalendarEventId,
            new CalendarEventStart(
                calendarEvent.Start.LocalDateTime,
                calendarEvent.Start.TimeZoneId),
            calendarEvent.ScheduledStartUtc,
            calendarEvent.Descriptions
                .Select(description => new LocalizedCalendarEventText(
                    description.Language,
                    description.Title,
                    description.Description))
                .ToArray(),
            detail.Platforms
                .Select(ToEventPlatformResponse)
                .ToArray());
    }

    /// <summary>
    /// Maps one event-platform projection item to its response shape: the
    /// platform id and type a client needs to drive the publish route, the
    /// publication status, and the precomputed <c>canPublish</c> action flag.
    /// Orphaned history rows set <c>platformDeletedUtc</c> and report
    /// <c>canPublish: false</c>.
    /// </summary>
    internal static EventPlatformResponse ToEventPlatformResponse(EventPlatformView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return new EventPlatformResponse(
            view.PlatformId,
            view.PlatformName,
            view.PlatformType.ToString(),
            view.Status.ToString(),
            view.ExternalResourceId,
            view.PublishedUtc,
            view.PlatformDeletedUtc,
            view.CanPublish);
    }

    private static string ToSortString(CalendarEventSortField sort) =>
        sort switch
        {
            CalendarEventSortField.TimeZone => "timeZone",
            CalendarEventSortField.Title => "title",
            _ => "scheduledStart"
        };

    private static string ToDirectionString(SortDirection direction) =>
        direction == SortDirection.Descending ? "desc" : "asc";

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

        if (value >= minValue && value <= maxValue)
        {
            return true;
        }

        error = new BadRequestObjectResult(
            $"Query parameter '{name}' must be between {minValue} and {maxValue}.");

        return false;

    }
}
