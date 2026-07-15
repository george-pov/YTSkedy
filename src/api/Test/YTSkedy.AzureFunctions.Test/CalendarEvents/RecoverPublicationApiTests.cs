using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using YTSkedy.AzureFunctions.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms.Publications;

namespace YTSkedy.AzureFunctions.Test.CalendarEvents;

public sealed class RecoverPublicationApiTests
{
    [Fact]
    public void ToResult_Recovered_ReturnsNoContent()
    {
        var result = Map(RecoverPublicationStatus.Recovered);

        Assert.IsType<NoContentResult>(result);
    }

    [Theory]
    [InlineData(RecoverPublicationStatus.EventNotFound)]
    [InlineData(RecoverPublicationStatus.PlatformNotFound)]
    [InlineData(RecoverPublicationStatus.PublicationNotFound)]
    public void ToResult_MissingState_ReturnsNotFound(RecoverPublicationStatus status)
    {
        Assert.IsType<NotFoundObjectResult>(Map(status));
    }

    [Theory]
    [InlineData(RecoverPublicationStatus.PlatformDeleted)]
    [InlineData(RecoverPublicationStatus.PastStart)]
    [InlineData(RecoverPublicationStatus.NotPublishing)]
    [InlineData(RecoverPublicationStatus.NotStale)]
    [InlineData(RecoverPublicationStatus.RowChanged)]
    public void ToResult_IneligibleOrChangedState_ReturnsConflict(
        RecoverPublicationStatus status)
    {
        var result = Assert.IsType<ConflictObjectResult>(Map(status));

        Assert.Contains("Reload", Assert.IsType<string>(result.Value), StringComparison.OrdinalIgnoreCase);
    }

    private static IActionResult Map(RecoverPublicationStatus status) =>
        RecoverPlatformPublicationApi.ToResult(
            new RecoverPublicationResult(status),
            "event-id",
            "platform-id");
}
