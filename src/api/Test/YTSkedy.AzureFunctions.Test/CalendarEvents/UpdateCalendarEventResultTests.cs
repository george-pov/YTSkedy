using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class UpdateCalendarEventResultTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";

    [Fact]
    public void ToUpdateResult_Updated_Returns200()
    {
        var result = CalendarEventsApi.ToUpdateResult(
            UpdateCalendarEventResult.Updated,
            CalendarEventId);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public void ToUpdateResult_NotFound_Returns404()
    {
        var result = CalendarEventsApi.ToUpdateResult(
            UpdateCalendarEventResult.NotFound,
            CalendarEventId);

        var notFound = Assert.IsType<NotFoundResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public void ToUpdateResult_Invalid_Returns400()
    {
        var result = CalendarEventsApi.ToUpdateResult(
            UpdateCalendarEventResult.Invalid("Text value is invalid."),
            CalendarEventId);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);
    }

    [Fact]
    public void UpdateCalendarEventAsync_HasPlatformPublications_Returns409()
    {
        var result = CalendarEventsApi.ToUpdateResult(
            UpdateCalendarEventResult.HasPlatformPublications,
            CalendarEventId);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.Equal(
            "Calendar event 'f81d4fae7dec11d0a76500a0c91e6bf6' has platform publications. " +
            "Delete platform publications before updating the event.",
            conflict.Value);
    }
}
