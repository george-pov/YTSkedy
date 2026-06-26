using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.AzureFunctions.Http;
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
    [Function("CreateCalendarEvent")]
    [RequiredScope("CalendarEvents.Write")]
    public async Task<IActionResult> CreateCalendarEventAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "calendar-events")]
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var body = await HttpJsonBody.ReadRequiredAsync<CreateCalendarEventRequest>(
            request,
            cancellationToken);
        if (body.Error is not null)
        {
            return body.Error;
        }

        var createRequest = body.Value!;
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
        var body = await HttpJsonBody.ReadRequiredAsync<UpdateCalendarEventRequest>(
            request,
            cancellationToken);
        if (body.Error is not null)
        {
            return body.Error;
        }

        var updateRequest = body.Value!;
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
            DeleteCalendarEventResult.HasPlatformPublications => new ConflictObjectResult(
                $"Calendar event '{calendarEventId}' has platform publications. " +
                "Delete platform publications before deleting the event."),
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

        if (!HttpQuery.TryParseOptionalInt(
                request,
                "page",
                out page,
                out var hasPage,
                out error,
                "Query parameter 'page' must be a non-negative integer."))
        {
            return false;
        }

        if (!hasPage)
        {
            page = 0;
        }

        if (!HttpQuery.TryParseOptionalInt(
                request,
                "pageSize",
                out pageSize,
                out var hasPageSize,
                out error,
                "Query parameter 'pageSize' must be an integer between 1 and 100."))
        {
            return false;
        }

        if (!hasPageSize)
        {
            pageSize = 10;
        }

        if (pageSize < 1 || pageSize > 100)
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

        if (!HttpQuery.TryGetSingleValue(request, "sort", out var sortValue, out error))
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

        if (!HttpQuery.TryGetSingleValue(request, "direction", out var directionValue, out error))
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

        if (!HttpQuery.TryParseRequiredInt(request, "year", out var parsedYear, out error))
        {
            return false;
        }

        if (!HttpQuery.TryValidateRange("year", parsedYear, 1000, 9999, out error))
        {
            return false;
        }

        if (!HttpQuery.TryParseRequiredInt(request, "month", out var parsedMonth, out error))
        {
            return false;
        }

        if (!HttpQuery.TryValidateRange("month", parsedMonth, 1, 12, out error))
        {
            return false;
        }

        year = parsedYear;
        month = parsedMonth;
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
    /// publication status, and the precomputed row action flags. Orphaned
    /// history rows set <c>platformDeletedUtc</c> and report both action flags
    /// as false.
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
            view.CanPublish,
            view.CanDeletePublication);
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

}
