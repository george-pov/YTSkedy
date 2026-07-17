using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerGuardTests
{
    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var scenario = new PublishHandlerScenario { CalendarEvent = null };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_MissingPlatform_ReturnsPlatformNotFound()
    {
        var scenario = new PublishHandlerScenario { SelectedPlatform = null };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PlatformNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_NoProviderForType_ReturnsProviderNotSupported()
    {
        var scenario = new PublishHandlerScenario();
        scenario.Publisher
            .SetupGet(candidate => candidate.Type)
            .Returns(PlatformType.WordPress);

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.ProviderNotSupported, result.Status);
    }

    [Fact]
    public async Task HandleAsync_OrphanedRow_ReturnsPlatformDeleted()
    {
        var scenario = new PublishHandlerScenario
        {
            ExistingPublication = Publication(
                PublishStatus.Published,
                platformDeletedUtc: Now)
        };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PlatformDeleted, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PublishedRow_ReturnsAlreadyPublished()
    {
        var scenario = new PublishHandlerScenario
        {
            ExistingPublication = Publication(PublishStatus.Published)
        };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.AlreadyPublished, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PublishingRow_ReturnsPublishInProgress()
    {
        var scenario = new PublishHandlerScenario
        {
            ExistingPublication = Publication(PublishStatus.Publishing)
        };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PublishInProgress, result.Status);
    }

    [Fact]
    public async Task HandleAsync_FailedRow_RetriesPublish()
    {
        var scenario = new PublishHandlerScenario
        {
            ExistingPublication = Publication(PublishStatus.Failed)
        };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.IsAny<PlatformPublicationAttempt>(),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_PastStart_ReturnsPastStart()
    {
        var scenario = new PublishHandlerScenario { CalendarEvent = Event(PastStart) };

        var result = await scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PastStart, result.Status);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = new PublishHandlerScenario().CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }
}
