using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class GetPublishingContentApiTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

    [Theory]
    [InlineData(PublishingContentType.Preview, "Preview")]
    [InlineData(PublishingContentType.Snapshot, "Snapshot")]
    public void ToResult_Found_Returns200WithContent(PublishingContentType type, string expectedType)
    {
        var result = type == PublishingContentType.Preview
            ? GetPublishingContentResult.Preview(
                new RenderedContent("Rendered title", "Rendered description"))
            : GetPublishingContentResult.Snapshot(
                new ContentSnapshot("Rendered title", "Rendered description"));

        var actionResult = GetPublishingContentApi.ToResult(
            result,
            CalendarEventId,
            PlatformId);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var body = Assert.IsType<RenderedPublishingContentResponse>(ok.Value);
        Assert.Equal(expectedType, body.Type);
        Assert.Equal("Rendered title", body.Title);
        Assert.Equal("Rendered description", body.Description);
    }

    [Theory]
    [InlineData(GetPublishingContentStatus.CalendarEventNotFound, StatusCodes.Status404NotFound)]
    [InlineData(GetPublishingContentStatus.PlatformNotFound, StatusCodes.Status404NotFound)]
    [InlineData(GetPublishingContentStatus.PreviewUnavailable, StatusCodes.Status409Conflict)]
    [InlineData(GetPublishingContentStatus.TemplateNotFound, StatusCodes.Status409Conflict)]
    [InlineData(GetPublishingContentStatus.EmptyTitle, StatusCodes.Status409Conflict)]
    public void ToResult_FailureStatus_MapsToStatusCode(
        GetPublishingContentStatus status,
        int expectedStatusCode)
    {
        var actionResult = GetPublishingContentApi.ToResult(
            GetPublishingContentResult.ForStatus(status),
            CalendarEventId,
            PlatformId);

        var statusCode = actionResult switch
        {
            ObjectResult objectResult => objectResult.StatusCode,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            _ => null
        };

        Assert.Equal(expectedStatusCode, statusCode);
    }
}
