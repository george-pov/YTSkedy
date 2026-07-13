using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Identity.Web.Resource;
using YTSkedy.AzureFunctions.Http;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.EventPlatforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.AzureFunctions.CalendarEvents;

public sealed class CalendarEventsApi(
    CreateCalendarEventHandler createHandler,
    ListEventsHandler listHandler,
    GetCalendarEventDetailsHandler getDetailsHandler,
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
        if (!TryBuildCreateCommand(createRequest, out var command, out var error))
        {
            return error;
        }

        var result = await createHandler.HandleAsync(command, cancellationToken);

        return ToCreateResult(result);
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
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "calendar-events/{calendarEventId:regex(^[0-9a-f]{{32}}$)}")]
        HttpRequest request,
        string calendarEventId,
        CancellationToken cancellationToken)
    {
        var details = await getDetailsHandler.HandleAsync(calendarEventId, cancellationToken);

        return details is null
            ? new NotFoundResult()
            : new OkObjectResult(ToDetailsResponse(details));
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
        if (!TryBuildUpdateCommand(calendarEventId, updateRequest, out var command, out var error))
        {
            return error;
        }

        var result = await updateHandler.HandleAsync(command, cancellationToken);

        return ToUpdateResult(result, calendarEventId);
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

    internal static IActionResult ToUpdateResult(
        UpdateCalendarEventResult result,
        string calendarEventId) =>
        result.Status switch
        {
            UpdateCalendarEventStatus.Updated => new OkObjectResult(
                new UpdateCalendarEventResponse(calendarEventId)),
            UpdateCalendarEventStatus.NotFound => new NotFoundResult(),
            UpdateCalendarEventStatus.HasPlatformPublications => new ConflictObjectResult(
                $"Calendar event '{calendarEventId}' has platform publications. " +
                "Delete platform publications before updating the event."),
            UpdateCalendarEventStatus.Invalid => new BadRequestObjectResult(result.ValidationError),
            UpdateCalendarEventStatus.DuplicateScheduledStart => new ConflictObjectResult(
                $"Calendar event scheduled for '{result.ScheduledStartUtc!.Value:o}' already exists."),
            _ => new StatusCodeResult(StatusCodes.Status500InternalServerError)
        };

    internal static IActionResult ToCreateResult(CreateCalendarEventResult result) =>
        result.Status switch
        {
            CreateCalendarEventStatus.Created => new OkObjectResult(
                new CreateCalendarEventResponse(result.CalendarEventId!)),
            CreateCalendarEventStatus.Invalid => new BadRequestObjectResult(result.ValidationError),
            CreateCalendarEventStatus.DuplicateScheduledStart => new ConflictObjectResult(
                $"Calendar event scheduled for '{result.ScheduledStartUtc!.Value:o}' already exists."),
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
                case "publicationstatus":
                    sort = CalendarEventSortField.PublicationStatus;
                    break;
                default:
                    error = new BadRequestObjectResult(
                        "Query parameter 'sort' must be 'scheduledStart', 'timeZone', 'title', " +
                        "or 'publicationStatus'.");
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
        CalendarEventListItem item)
    {
        var calendarEvent = item.Event;

        return new(
            calendarEvent.CalendarEventId,
            new CalendarEventStart(
                calendarEvent.Start.LocalDateTime,
                calendarEvent.Start.TimeZoneId),
            calendarEvent.ScheduledStartUtc,
            calendarEvent.Text.DisplayTitle,
            ToPublishingStatusString(item.PublicationStatus),
            ToTextResponse(calendarEvent.Text));
    }

    internal static string ToPublishingStatusString(PublishingStatus status) =>
        status switch
        {
            PublishingStatus.NotPublished => "NotPublished",
            PublishingStatus.PartiallyPublished => "PartiallyPublished",
            PublishingStatus.FullyPublished => "FullyPublished",
            PublishingStatus.Failed => "Failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

    /// <summary>
    /// Maps the calendar event details read model to the get-by-id response. The
    /// event fields mirror one list item; root action flags describe event text
    /// update and event delete eligibility; <c>platforms</c> is mapped by
    /// <see cref="ToEventPlatformResponse"/>. This details response is the only
    /// place the per-platform publication state is exposed over HTTP.
    /// </summary>
    internal static CalendarEventDetailsResponse ToDetailsResponse(CalendarEventDetailsView details)
    {
        ArgumentNullException.ThrowIfNull(details);

        var calendarEvent = details.Event;

        return new CalendarEventDetailsResponse(
            calendarEvent.CalendarEventId,
            new CalendarEventStart(
                calendarEvent.Start.LocalDateTime,
                calendarEvent.Start.TimeZoneId),
            calendarEvent.ScheduledStartUtc,
            calendarEvent.Text.DisplayTitle,
            details.CanUpdate,
            details.CanDelete,
            details.Thumbnail is null
                ? null
                : CalendarEventThumbnailsApi.ToThumbnailResponse(details.Thumbnail),
            details.CanUpdateThumbnail,
            ToTextResponse(calendarEvent.Text),
            details.Platforms
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
            view.ThumbnailStatus?.ToString(),
            view.PublishedUtc,
            view.PlatformDeletedUtc,
            view.CanPublish,
            view.CanDeletePublication,
            view.CanPreviewPublishingContent);
    }

    private static string ToSortString(CalendarEventSortField sort) =>
        sort switch
        {
            CalendarEventSortField.TimeZone => "timeZone",
            CalendarEventSortField.Title => "title",
            CalendarEventSortField.PublicationStatus => "publicationStatus",
            _ => "scheduledStart"
        };

    private static string ToDirectionString(SortDirection direction) =>
        direction == SortDirection.Descending ? "desc" : "asc";

    /// <summary>
    /// Validates a create request at the API boundary and maps it to a command.
    /// Structural failures (missing start, malformed text entries) yield a
    /// <c>400 Bad Request</c> through <paramref name="error"/>. Value validity
    /// against the configured fields is state-dependent and checked in the handler.
    /// </summary>
    internal static bool TryBuildCreateCommand(
        CreateCalendarEventRequest request,
        out CreateCalendarEventCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        if (request.Start is null)
        {
            error = InvalidStartResult();
            return false;
        }

        if (!TryBuildEventTextValues(request.Texts, out var texts, out error))
        {
            return false;
        }

        command = new CreateCalendarEventCommand(
            new ScheduledStart(
                request.Start.LocalDateTime,
                request.Start.TimeZoneId),
            texts);
        return true;
    }

    /// <summary>
    /// Validates an update request and its route id at the API boundary and maps
    /// them to a command. Structural failures yield a <c>400 Bad Request</c>
    /// through <paramref name="error"/>.
    /// </summary>
    internal static bool TryBuildUpdateCommand(
        string calendarEventId,
        UpdateCalendarEventRequest request,
        out UpdateCalendarEventCommand command,
        out IActionResult error)
    {
        ArgumentNullException.ThrowIfNull(request);

        command = default!;
        error = new EmptyResult();

        if (request.Start is null)
        {
            error = InvalidStartResult();
            return false;
        }

        if (!TryBuildEventTextValues(request.Texts, out var texts, out error))
        {
            return false;
        }

        command = new UpdateCalendarEventCommand(
            calendarEventId,
            new ScheduledStart(
                request.Start.LocalDateTime,
                request.Start.TimeZoneId),
            texts);
        return true;
    }

    private static bool TryBuildEventTextValues(
        IReadOnlyList<EventTextPayload> texts,
        out EventTextValue[] values,
        out IActionResult error)
    {
        values = [];
        error = new EmptyResult();

        if (texts is null)
        {
            error = InvalidTextsResult();
            return false;
        }

        var built = new List<EventTextValue>();
        foreach (var text in texts)
        {
            if (text is null)
            {
                error = InvalidTextsResult();
                return false;
            }

            try
            {
                built.Add(new EventTextValue(text.FieldKey, text.Value));
            }
            catch (ArgumentException)
            {
                error = InvalidTextsResult();
                return false;
            }
        }

        values = built.ToArray();
        return true;
    }

    private static IActionResult InvalidStartResult() =>
        new BadRequestObjectResult(
            "Start local date-time and time zone id are required.");

    private static IActionResult InvalidTextsResult() =>
        new BadRequestObjectResult(
            "Text entries must each have a field key and value.");

    private static EventTextResponse[] ToTextResponse(EventTextSnapshot text)
    {
        var valuesByKey = text.Values.ToDictionary(
            value => value.FieldKey,
            value => value.Value,
            StringComparer.Ordinal);

        return text.Fields
            .Select(field => new EventTextResponse(
                field.FieldKey,
                field.Label,
                field.Type.ToString(),
                field.MaxLength,
                valuesByKey.TryGetValue(field.FieldKey, out var value)
                    ? value
                    : string.Empty))
            .ToArray();
    }
}
