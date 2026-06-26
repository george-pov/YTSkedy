using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class DeletePlatformPublicationApiTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

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

        var statusCode = actionResult switch
        {
            ObjectResult objectResult => objectResult.StatusCode,
            StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
            _ => null
        };

        Assert.Equal(expectedStatusCode, statusCode);
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
            CanDeletePublication: false);
}
