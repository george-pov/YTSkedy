using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class PublishEventPlatformApiTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

    [Fact]
    public void ToResult_Published_Returns200WithPublishBody()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var result = PublishedResult(
            "Main YouTube channel",
            PlatformType.YouTube,
            "yt-broadcast-id",
            publishedUtc);

        var actionResult = PublishEventPlatformApi.ToResult(result, CalendarEventId, PlatformId);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var body = Assert.IsType<EventPlatformResponse>(ok.Value);
        Assert.Equal(PlatformId, body.PlatformId);
        Assert.Equal("Main YouTube channel", body.PlatformName);
        Assert.Equal("YouTube", body.PlatformType);
        Assert.Equal("Published", body.Status);
        Assert.Equal("yt-broadcast-id", body.ExternalResourceId);
        Assert.Equal("Applied", body.ThumbnailStatus);
        Assert.Equal(publishedUtc, body.PublishedUtc);
        Assert.Null(body.PlatformDeletedUtc);
        Assert.False(body.CanPublish);
        Assert.True(body.CanDeletePublication);
        Assert.True(body.CanPreviewPublishingContent);
    }

    [Fact]
    public void ToResult_WordPressPublished_Returns200WithWordPressPublishBody()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var result = PublishedResult(
            "Company blog",
            PlatformType.WordPress,
            "123",
            publishedUtc);

        var actionResult = PublishEventPlatformApi.ToResult(result, CalendarEventId, PlatformId);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var body = Assert.IsType<EventPlatformResponse>(ok.Value);
        Assert.Equal("Company blog", body.PlatformName);
        Assert.Equal("WordPress", body.PlatformType);
        Assert.Equal("Published", body.Status);
        Assert.Equal("123", body.ExternalResourceId);
        Assert.Null(body.ThumbnailStatus);
        Assert.Equal(publishedUtc, body.PublishedUtc);
        Assert.Null(body.PlatformDeletedUtc);
        Assert.False(body.CanPublish);
        Assert.True(body.CanDeletePublication);
        Assert.True(body.CanPreviewPublishingContent);
    }

    [Theory]
    [InlineData(PublishResultStatus.EventNotFound, StatusCodes.Status404NotFound)]
    [InlineData(PublishResultStatus.PlatformNotFound, StatusCodes.Status404NotFound)]
    [InlineData(PublishResultStatus.PastStart, StatusCodes.Status400BadRequest)]
    [InlineData(PublishResultStatus.InvalidPublishingContent, StatusCodes.Status409Conflict)]
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

    private static PublishResult PublishedResult(
        string platformName,
        PlatformType platformType,
        string externalResourceId,
        DateTimeOffset publishedUtc) =>
        PublishResult.Published(
            new EventPlatformView(
                PlatformId,
                platformName,
                platformType,
                PublishStatus.Published,
            externalResourceId,
            publishedUtc,
            null,
            CanPublish: false,
            CanDeletePublication: true,
            CanPreviewPublishingContent: true,
            ThumbnailStatus: platformType == PlatformType.YouTube
                ? ThumbnailPublishStatus.Applied
                : null));
}
