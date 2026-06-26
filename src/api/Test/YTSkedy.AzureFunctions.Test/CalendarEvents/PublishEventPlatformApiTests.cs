using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class PublishEventPlatformApiTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

    [Fact]
    public void ToResult_Published_Returns200WithPublishBody()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var result = PublishResult.Published(
            "Main YouTube channel",
            PlatformType.YouTube,
            "yt-broadcast-id",
            publishedUtc);

        var actionResult = PublishEventPlatformApi.ToResult(result, CalendarEventId, PlatformId);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var body = Assert.IsType<PublishEventPlatformResponse>(ok.Value);
        Assert.Equal(CalendarEventId, body.CalendarEventId);
        Assert.Equal(PlatformId, body.PlatformId);
        Assert.Equal("Main YouTube channel", body.PlatformName);
        Assert.Equal("YouTube", body.PlatformType);
        Assert.Equal("Published", body.Status);
        Assert.Equal("yt-broadcast-id", body.ExternalResourceId);
        Assert.Equal(publishedUtc, body.PublishedUtc);
    }

    [Fact]
    public void ToResult_WordPressPublished_Returns200WithWordPressPublishBody()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var result = PublishResult.Published(
            "Company blog",
            PlatformType.WordPress,
            "123",
            publishedUtc);

        var actionResult = PublishEventPlatformApi.ToResult(result, CalendarEventId, PlatformId);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var body = Assert.IsType<PublishEventPlatformResponse>(ok.Value);
        Assert.Equal("Company blog", body.PlatformName);
        Assert.Equal("WordPress", body.PlatformType);
        Assert.Equal("Published", body.Status);
        Assert.Equal("123", body.ExternalResourceId);
        Assert.Equal(publishedUtc, body.PublishedUtc);
    }

    [Theory]
    [InlineData(PublishResultStatus.EventNotFound, StatusCodes.Status404NotFound)]
    [InlineData(PublishResultStatus.PlatformNotFound, StatusCodes.Status404NotFound)]
    [InlineData(PublishResultStatus.PastStart, StatusCodes.Status400BadRequest)]
    [InlineData(PublishResultStatus.MissingEnglishTitle, StatusCodes.Status400BadRequest)]
    [InlineData(PublishResultStatus.AlreadyPublished, StatusCodes.Status409Conflict)]
    [InlineData(PublishResultStatus.PublishInProgress, StatusCodes.Status409Conflict)]
    [InlineData(PublishResultStatus.PlatformDeleted, StatusCodes.Status409Conflict)]
    [InlineData(PublishResultStatus.ProviderNotSupported, StatusCodes.Status501NotImplemented)]
    [InlineData(PublishResultStatus.ProviderFailed, StatusCodes.Status502BadGateway)]
    [InlineData(PublishResultStatus.FinalizeFailed, StatusCodes.Status500InternalServerError)]
    public void ToResult_FailureStatus_MapsToStatusCode(
        PublishResultStatus status,
        int expectedStatusCode)
    {
        var actionResult = PublishEventPlatformApi.ToResult(
            PublishResult.ForStatus(status),
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
