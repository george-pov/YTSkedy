using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

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
}