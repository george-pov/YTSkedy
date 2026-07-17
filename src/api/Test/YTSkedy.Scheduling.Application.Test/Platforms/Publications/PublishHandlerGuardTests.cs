using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerGuardTests
{
    private readonly PublishHandlerScenario _scenario = new();

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        _scenario.CalendarEvent = null;

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_MissingPlatform_ReturnsPlatformNotFound()
    {
        _scenario.SelectedPlatform = null;

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PlatformNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_NoProviderForType_ReturnsProviderNotSupported()
    {
        _scenario.Publisher
            .SetupGet(candidate => candidate.Type)
            .Returns(PlatformType.WordPress);

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.ProviderNotSupported, result.Status);
    }

    [Fact]
    public async Task HandleAsync_OrphanedRow_ReturnsPlatformDeleted()
    {
        _scenario.ExistingPublication = Publication(
            PublishStatus.Published,
            platformDeletedUtc: Now);

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PlatformDeleted, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PublishedRow_ReturnsAlreadyPublished()
    {
        _scenario.ExistingPublication = Publication(PublishStatus.Published);

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.AlreadyPublished, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PublishingRow_ReturnsPublishInProgress()
    {
        _scenario.ExistingPublication = Publication(PublishStatus.Publishing);

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PublishInProgress, result.Status);
    }

    [Fact]
    public async Task HandleAsync_FailedRow_RetriesPublish()
    {
        _scenario.ExistingPublication = Publication(PublishStatus.Failed);

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.Published, result.Status);
        _scenario.PublicationAttempts.Verify(candidate => candidate.StartPublishingAsync(
            It.IsAny<PlatformPublicationAttempt>(),
            It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task HandleAsync_PastStart_ReturnsPastStart()
    {
        _scenario.CalendarEvent = Event(PastStart);

        var result = await _scenario.HandleAsync();

        Assert.Equal(PublishResultStatus.PastStart, result.Status);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = _scenario.CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }
}
