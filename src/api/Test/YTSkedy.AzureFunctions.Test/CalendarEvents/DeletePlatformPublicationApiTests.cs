using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.TestSupport;
using YTSkedy.TestSupport;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class DeletePlatformPublicationApiTests
{
    private const string CalendarEventId = SchedulingSampleIds.CalendarEventId;
    private const string PlatformId = SchedulingSampleIds.PlatformId;

    [Theory]
    [InlineData(DeletePublicationStatus.Deleted)]
    [InlineData(DeletePublicationStatus.AlreadyNotPublished)]
    public void ToResult_Success_Returns200WithEventPlatformBody(DeletePublicationStatus status)
    {
        var result = DeletePublicationResult.Success(status, NotPublishedRow());

        var actionResult = DeletePlatformPublicationApi.ToResult(result, CalendarEventId, PlatformId);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var body = Assert.IsType<EventPlatformResponse>(ok.Value);
        Assert.Equal(PlatformId, body.PlatformId);
        Assert.Equal("NotPublished", body.Status);
        Assert.True(body.CanPublish);
        Assert.False(body.CanDeletePublication);
        Assert.True(body.CanPreviewPublishingContent);
    }

    [Theory]
    [InlineData(DeletePublicationStatus.EventNotFound, StatusCodes.Status404NotFound)]
    [InlineData(DeletePublicationStatus.PlatformNotFound, StatusCodes.Status404NotFound)]
    [InlineData(DeletePublicationStatus.Orphaned, StatusCodes.Status409Conflict)]
    [InlineData(DeletePublicationStatus.PastStart, StatusCodes.Status409Conflict)]
    [InlineData(DeletePublicationStatus.MissingExternalResourceId, StatusCodes.Status409Conflict)]
    [InlineData(DeletePublicationStatus.TargetMismatch, StatusCodes.Status409Conflict)]
    [InlineData(DeletePublicationStatus.PublishInProgress, StatusCodes.Status409Conflict)]
    [InlineData(DeletePublicationStatus.ProviderStateConflict, StatusCodes.Status409Conflict)]
    [InlineData(DeletePublicationStatus.RowChanged, StatusCodes.Status409Conflict)]
    [InlineData(DeletePublicationStatus.ProviderNotSupported, StatusCodes.Status501NotImplemented)]
    [InlineData(DeletePublicationStatus.ProviderFailed, StatusCodes.Status502BadGateway)]
    public void ToResult_FailureStatus_MapsToStatusCode(
        DeletePublicationStatus status,
        int expectedStatusCode)
    {
        var actionResult = DeletePlatformPublicationApi.ToResult(
            DeletePublicationResult.ForStatus(status),
            CalendarEventId,
            PlatformId);

        Assert.Equal(expectedStatusCode, ActionResultAssertions.StatusCode(actionResult));
    }

    private static EventPlatformView NotPublishedRow() =>
        new(
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            PublishStatus.NotPublished,
            null,
            null,
            null,
            CanPublish: true,
            CanDeletePublication: false,
            CanPreviewPublishingContent: true);
}
