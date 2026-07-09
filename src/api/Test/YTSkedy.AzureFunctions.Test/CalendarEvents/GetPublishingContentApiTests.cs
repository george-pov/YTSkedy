using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Content;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class GetPublishingContentApiTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;

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

        Assert.Equal(expectedStatusCode, ActionResultAssertions.StatusCode(actionResult));
    }
}
