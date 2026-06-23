using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerTests
{
    private const string CalendarEventId = "20260615T170000Z";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStart = new(2026, 6, 25, 17, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastStart = new(2026, 6, 1, 17, 0, 0, TimeSpan.Zero);

    private static readonly YouTubeSettings Settings = new("main-youtube-channel", "private", false);

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var handler = CreateHandler(calendarEvent: null, platform: Platform(), publisher: new FakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.EventNotFound, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_MissingPlatform_ReturnsPlatformNotFound()
    {
        var handler = CreateHandler(Event(FutureStart), platform: null, publisher: new FakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.PlatformNotFound, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_NoProviderForType_ReturnsProviderNotSupported()
    {
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher: null);

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.ProviderNotSupported, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_OrphanedRow_ReturnsPlatformDeleted()
    {
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new FakePublisher(),
            existing: Publication(PublishStatus.Published, platformDeletedUtc: Now));

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.PlatformDeleted, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_PublishedRow_ReturnsAlreadyPublished()
    {
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new FakePublisher(),
            existing: Publication(PublishStatus.Published));

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.AlreadyPublished, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_PublishingRow_ReturnsPublishInProgress()
    {
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new FakePublisher(),
            existing: Publication(PublishStatus.Publishing));

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.PublishInProgress, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_PastStart_ReturnsPastStart()
    {
        var handler = CreateHandler(Event(PastStart), Platform(), new FakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.PastStart, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_NoEnglishTitle_ReturnsMissingEnglishTitle()
    {
        var handler = CreateHandler(
            Event(FutureStart, [new LocalizedDescription("ru", "Russian title", null)]),
            Platform(),
            new FakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.MissingEnglishTitle, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_BlankEnglishTitle_ReturnsMissingEnglishTitle()
    {
        var handler = CreateHandler(
            Event(FutureStart, [new LocalizedDescription("en", "   ", "description")]),
            Platform(),
            new FakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.MissingEnglishTitle, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_ReservationConflict_ReturnsPublishInProgress()
    {
        var repository = new FakePublicationRepository { ReserveResult = ReservePublicationResult.Conflict };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new FakePublisher(),
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.PublishInProgress, result.Outcome);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_ReleasesReservationAndReturnsProviderFailed()
    {
        var repository = new FakePublicationRepository();
        var publisher = new FakePublisher { Throws = new PlatformPublishException("provider down") };
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher, repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.ProviderFailed, result.Outcome);
        Assert.True(repository.ReleaseCalled);
        Assert.False(repository.MarkPublishedCalled);
    }

    [Fact]
    public async Task HandleAsync_FinalizeReturnsNull_ReturnsFinalizeFailed()
    {
        var repository = new FakePublicationRepository { MarkPublishedResult = null };
        var publisher = new FakePublisher { Result = new PlatformPublishResult("yt-broadcast-id") };
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher, repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.FinalizeFailed, result.Outcome);
        Assert.False(repository.ReleaseCalled);
    }

    [Fact]
    public async Task HandleAsync_Success_ReservesPublishesFinalizesAndReturnsPublished()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);
        var repository = new FakePublicationRepository { MarkPublishedResult = publishedUtc };
        var publisher = new FakePublisher { Result = new PlatformPublishResult("yt-broadcast-id") };
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher, repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishOutcome.Published, result.Outcome);
        Assert.Equal("Main YouTube channel", result.PlatformName);
        Assert.Equal(PlatformType.YouTube, result.PlatformType);
        Assert.Equal("yt-broadcast-id", result.ExternalResourceId);
        Assert.Equal(publishedUtc, result.PublishedUtc);

        Assert.True(repository.Reserved);
        Assert.Equal("yt-broadcast-id", repository.MarkedExternalResourceId);
        Assert.False(repository.ReleaseCalled);

        // The provider receives the English content and the stored future start.
        Assert.Equal("English title", publisher.Request!.Title);
        Assert.Equal("English description", publisher.Request.Description);
        Assert.Equal(FutureStart, publisher.Request.ScheduledStartUtc);
        Assert.Same(Settings, publisher.Request.PublishSettings);
    }

    [Fact]
    public async Task HandleAsync_NullCommand_Throws()
    {
        var handler = CreateHandler(Event(FutureStart), Platform(), new FakePublisher());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static Task<PublishResult> Handle(PublishHandler handler) =>
        handler.HandleAsync(new PublishCommand(CalendarEventId, PlatformId), CancellationToken.None);

    private static PublishHandler CreateHandler(
        CalendarEventView? calendarEvent,
        PlatformView? platform,
        IPlatformPublisher? publisher,
        PlatformPublication? existing = null,
        FakePublicationRepository? repository = null) =>
        new(
            new FakeCalendarEventReader(calendarEvent),
            new FakePlatformReader(platform),
            new FakePublicationReader(existing),
            repository ?? new FakePublicationRepository(),
            new FakeSelector(publisher),
            new FixedTimeProvider(Now),
            NullLogger<PublishHandler>.Instance);

    private static CalendarEventView Event(
        DateTimeOffset startUtc,
        IReadOnlyList<LocalizedDescription>? descriptions = null) =>
        new(
            CalendarEventId,
            new ScheduledStart(startUtc.UtcDateTime, "UTC"),
            startUtc,
            descriptions ?? [new LocalizedDescription("en", "English title", "English description")]);

    private static PlatformView Platform() =>
        new(PlatformId, "Main YouTube channel", PlatformType.YouTube, Settings);

    private static PlatformPublication Publication(
        PublishStatus status,
        DateTimeOffset? platformDeletedUtc = null) =>
        new(
            CalendarEventId,
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            status,
            null,
            null,
            platformDeletedUtc,
            Now);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeCalendarEventReader(CalendarEventView? calendarEvent) : ICalendarEventReader
    {
        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(calendarEvent);
    }

    private sealed class FakePlatformReader(PlatformView? platform) : IPlatformReader
    {
        public Task<IReadOnlyList<PlatformView>> ListAsync(
            PlatformType? type,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlatformView?> GetAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            Task.FromResult(platform);
    }

    private sealed class FakePublicationReader(PlatformPublication? existing) : IPlatformPublicationReader
    {
        public Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlatformPublication?> GetAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            Task.FromResult(existing);

        public Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePublicationRepository : IPlatformPublicationRepository
    {
        public ReservePublicationResult ReserveResult { get; init; } = ReservePublicationResult.Reserved;

        public DateTimeOffset? MarkPublishedResult { get; init; } =
            new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);

        public bool Reserved { get; private set; }

        public bool ReleaseCalled { get; private set; }

        public bool MarkPublishedCalled { get; private set; }

        public string? MarkedExternalResourceId { get; private set; }

        public Task<ReservePublicationResult> ReserveAsync(
            PlatformPublicationReservation reservation,
            CancellationToken cancellationToken)
        {
            Reserved = ReserveResult == ReservePublicationResult.Reserved;

            return Task.FromResult(ReserveResult);
        }

        public Task ReleaseAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken)
        {
            ReleaseCalled = true;

            return Task.CompletedTask;
        }

        public Task<DateTimeOffset?> MarkPublishedAsync(
            string calendarEventId,
            string platformId,
            string externalResourceId,
            CancellationToken cancellationToken)
        {
            MarkPublishedCalled = true;
            MarkedExternalResourceId = externalResourceId;

            return Task.FromResult(MarkPublishedResult);
        }

        public Task<int> OrphanPublishedByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSelector(IPlatformPublisher? publisher) : IPlatformPublisherSelector
    {
        public IPlatformPublisher? Find(PlatformType type) => publisher;
    }

    private sealed class FakePublisher : IPlatformPublisher
    {
        public PlatformPublishResult? Result { get; init; }

        public Exception? Throws { get; init; }

        public PlatformPublishRequest? Request { get; private set; }

        public PlatformType Type => PlatformType.YouTube;

        public Task<PlatformPublishResult> PublishAsync(
            PlatformPublishRequest request,
            CancellationToken cancellationToken)
        {
            Request = request;

            if (Throws is not null)
            {
                throw Throws;
            }

            return Task.FromResult(Result ?? new PlatformPublishResult("yt-broadcast-id"));
        }
    }
}
