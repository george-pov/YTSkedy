using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.TestSupport;

namespace YTSkedy.Scheduling.Application.Test;

public sealed class RecoverPublicationHandlerTests
{
    private static readonly DateTimeOffset Now = PublishHandlerScenario.Now;
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(5);
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<IPlatformReader> _platforms = new();
    private readonly Mock<IPlatformPublicationReader> _publications = new();
    private readonly Mock<IPublicationAttemptWriter> _publicationAttempts = new();
    private readonly RecoverPublicationHandler _handler;

    public RecoverPublicationHandlerTests()
    {
        _handler = new RecoverPublicationHandler(
            _calendarEvents.Object,
            _platforms.Object,
            _publications.Object,
            _publicationAttempts.Object,
            new PublicationExecutionSettings(
                TimeSpan.FromMinutes(2),
                TimeSpan.FromSeconds(15),
                StaleAfter),
            new FixedTimeProvider(Now),
            NullLogger<RecoverPublicationHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ExactlyStalePublishingRow_RecoversObservedRow()
    {
        var updatedUtc = Now - StaleAfter;
        var repository = PublicationAttempts();
        var handler = CreateHandler(
            PublishHandlerScenario.Event(PublishHandlerScenario.FutureStart),
            PublishHandlerScenario.Platform(),
            Publication(PublishStatus.Publishing, updatedUtc),
            repository);

        var result = await Handle(handler);

        Assert.Equal(RecoverPublicationStatus.Recovered, result.Status);
        repository.Verify(candidate => candidate.RecoverStalePublishingAsync(
            PublishHandlerScenario.CalendarEventId,
            PublishHandlerScenario.PlatformId,
            updatedUtc,
            CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_YoungPublishingRow_ReturnsNotStale()
    {
        var handler = CreateHandler(
            PublishHandlerScenario.Event(PublishHandlerScenario.FutureStart),
            PublishHandlerScenario.Platform(),
            Publication(PublishStatus.Publishing, Now - StaleAfter + TimeSpan.FromSeconds(1)));

        var result = await Handle(handler);

        Assert.Equal(RecoverPublicationStatus.NotStale, result.Status);
    }

    [Theory]
    [InlineData(PublishStatus.NotPublished)]
    [InlineData(PublishStatus.Published)]
    [InlineData(PublishStatus.Failed)]
    public async Task HandleAsync_NonPublishingRow_ReturnsNotPublishing(PublishStatus status)
    {
        var handler = CreateHandler(
            PublishHandlerScenario.Event(PublishHandlerScenario.FutureStart),
            PublishHandlerScenario.Platform(),
            Publication(status, Now - StaleAfter));

        var result = await Handle(handler);

        Assert.Equal(RecoverPublicationStatus.NotPublishing, result.Status);
    }

    [Fact]
    public async Task HandleAsync_OrphanedRow_ReturnsPlatformDeleted()
    {
        var publication = Publication(PublishStatus.Publishing, Now - StaleAfter) with
        {
            PlatformDeletedUtc = Now - TimeSpan.FromMinutes(1)
        };
        var handler = CreateHandler(
            PublishHandlerScenario.Event(PublishHandlerScenario.FutureStart),
            platform: null,
            publication);

        var result = await Handle(handler);

        Assert.Equal(RecoverPublicationStatus.PlatformDeleted, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PastEvent_ReturnsPastStart()
    {
        var handler = CreateHandler(
            PublishHandlerScenario.Event(PublishHandlerScenario.PastStart),
            PublishHandlerScenario.Platform(),
            Publication(PublishStatus.Publishing, Now - StaleAfter));

        var result = await Handle(handler);

        Assert.Equal(RecoverPublicationStatus.PastStart, result.Status);
    }

    [Fact]
    public async Task HandleAsync_RowChanged_ReturnsRowChanged()
    {
        var repository = PublicationAttempts(RecoverStalePublishingResult.Changed);
        var handler = CreateHandler(
            PublishHandlerScenario.Event(PublishHandlerScenario.FutureStart),
            PublishHandlerScenario.Platform(),
            Publication(PublishStatus.Publishing, Now - StaleAfter),
            repository);

        var result = await Handle(handler);

        Assert.Equal(RecoverPublicationStatus.RowChanged, result.Status);
    }

    [Fact]
    public async Task HandleAsync_MissingPublicationAndActivePlatform_ReturnsPublicationNotFound()
    {
        var handler = CreateHandler(
            PublishHandlerScenario.Event(PublishHandlerScenario.FutureStart),
            PublishHandlerScenario.Platform(),
            publication: null);

        var result = await Handle(handler);

        Assert.Equal(RecoverPublicationStatus.PublicationNotFound, result.Status);
    }

    private RecoverPublicationHandler CreateHandler(
        Domain.CalendarEvents.CalendarEventView? calendarEvent,
        PlatformView? platform,
        PlatformPublication? publication,
        Mock<IPublicationAttemptWriter>? repository = null)
    {
        _calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                PublishHandlerScenario.CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        _platforms
            .Setup(candidate => candidate.GetAsync(
                PublishHandlerScenario.PlatformId,
                CancellationToken.None))
            .ReturnsAsync(platform);
        _publications
            .Setup(candidate => candidate.GetAsync(
                PublishHandlerScenario.CalendarEventId,
                PublishHandlerScenario.PlatformId,
                CancellationToken.None))
            .ReturnsAsync(publication);

        if (repository is null)
        {
            PublicationAttempts();
        }

        return _handler;
    }

    private static Task<RecoverPublicationResult> Handle(RecoverPublicationHandler handler) =>
        handler.HandleAsync(
            new RecoverPublicationCommand(
                PublishHandlerScenario.CalendarEventId,
                PublishHandlerScenario.PlatformId),
            CancellationToken.None);

    private static PlatformPublication Publication(
        PublishStatus status,
        DateTimeOffset updatedUtc) =>
        ApplicationTestData.Publication(
            status,
            calendarEventId: PublishHandlerScenario.CalendarEventId,
            platformId: PublishHandlerScenario.PlatformId,
            externalResourceId: "checkpoint-id",
            updatedUtc: updatedUtc,
            contentSnapshot: new ContentSnapshot("Stored title", "Stored description"));

    private Mock<IPublicationAttemptWriter> PublicationAttempts(
        RecoverStalePublishingResult? result = null)
    {
        _publicationAttempts
            .Setup(candidate => candidate.RecoverStalePublishingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? RecoverStalePublishingResult.Recovered);
        return _publicationAttempts;
    }
}
