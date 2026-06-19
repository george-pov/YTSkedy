using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

/// <summary>
/// Maps the delete use-case outcomes to their HTTP results without constructing
/// the Functions host or live YouTube resources. The mapping is the integration
/// contract surface for <c>DELETE /api/calendar-events/{calendarEventId}</c>.
/// </summary>
public sealed class DeleteCalendarEventResultTests
{
    private const string CalendarEventId = "20260615T170000Z";

    [Fact]
    public void ToDeleteResult_Deleted_Returns204NoContent()
    {
        var result = CalendarEventsApi.ToDeleteResult(
            DeleteCalendarEventResult.Deleted,
            CalendarEventId);

        var noContent = Assert.IsType<NoContentResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, noContent.StatusCode);
    }

    [Fact]
    public void ToDeleteResult_NotFound_Returns404()
    {
        var result = CalendarEventsApi.ToDeleteResult(
            DeleteCalendarEventResult.NotFound,
            CalendarEventId);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void ToDeleteResult_NotDeletable_Returns409WithNonDraftOnlyWording()
    {
        var result = CalendarEventsApi.ToDeleteResult(
            DeleteCalendarEventResult.NotDeletable,
            CalendarEventId);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        var message = Assert.IsType<string>(conflict.Value);
        Assert.DoesNotContain("draft", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("current state", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToDeleteResult_MissingYouTubeBroadcastId_Returns409WithDiagnosticWording()
    {
        var result = CalendarEventsApi.ToDeleteResult(
            DeleteCalendarEventResult.MissingYouTubeBroadcastId,
            CalendarEventId);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        var message = Assert.IsType<string>(conflict.Value);
        Assert.Contains("no YouTube broadcast id is recorded", message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToDeleteResult_YouTubeDeleteFailed_Returns502BadGateway()
    {
        var result = CalendarEventsApi.ToDeleteResult(
            DeleteCalendarEventResult.YouTubeDeleteFailed,
            CalendarEventId);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status502BadGateway, objectResult.StatusCode);
    }
}
