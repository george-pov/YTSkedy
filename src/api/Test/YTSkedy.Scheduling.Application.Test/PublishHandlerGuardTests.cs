using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.Platforms;
using static YTSkedy.Scheduling.Application.Test.PublishHandlerScenario;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerGuardTests
{
    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var handler = CreateHandler(
            calendarEvent: null,
            platform: Platform(),
            publisher: new PublishFakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_MissingPlatform_ReturnsPlatformNotFound()
    {
        var handler = CreateHandler(
            Event(FutureStart),
            platform: null,
            publisher: new PublishFakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PlatformNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_NoProviderForType_ReturnsProviderNotSupported()
    {
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher: null);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.ProviderNotSupported, result.Status);
    }

    [Fact]
    public async Task HandleAsync_OrphanedRow_ReturnsPlatformDeleted()
    {
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            existing: Publication(PublishStatus.Published, platformDeletedUtc: Now));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PlatformDeleted, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PublishedRow_ReturnsAlreadyPublished()
    {
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            existing: Publication(PublishStatus.Published));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.AlreadyPublished, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PublishingRow_ReturnsPublishInProgress()
    {
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new PublishFakePublisher(),
            existing: Publication(PublishStatus.Publishing));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PublishInProgress, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PastStart_ReturnsPastStart()
    {
        var handler = CreateHandler(Event(PastStart), Platform(), new PublishFakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PastStart, result.Status);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = CreateHandler(Event(FutureStart), Platform(), new PublishFakePublisher());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }
}
