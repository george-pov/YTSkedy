using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Test;

public class DeletePublicationHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string ExternalResourceId = "yt-broadcast-id";

    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStart = new(2026, 6, 15, 17, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastStart = Now;
    private static readonly YouTubeSettings Settings = new(
        new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
        "private",
        false);

    [Fact]
    public async Task DeletePublication_MissingEvent_ReturnsEventNotFound()
    {
        var handler = CreateHandler(hasCalendarEvent: false);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task DeletePublication_MissingPlatformAndMissingRow_ReturnsPlatformNotFound()
    {
        var handler = CreateHandler(hasPlatform: false, hasPublication: false);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.PlatformNotFound, result.Status);
    }

    [Fact]
    public async Task DeletePublication_MissingPlatformWithOrphanRow_ReturnsOrphaned()
    {
        var handler = CreateHandler(
            hasPlatform: false,
            publication: Publication(PublishStatus.Published, platformDeletedUtc: Now));

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.Orphaned, result.Status);
    }

    [Fact]
    public async Task DeletePublication_MissingRow_ReturnsNotPublishedRowWithoutProviderCall()
    {
        var repository = new FakePublicationRepository();
        var deleter = new FakePublicationDeleter();
        var handler = CreateHandler(
            publication: null,
            hasPublication: false,
            repository: repository,
            deleter: deleter);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.AlreadyNotPublished, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal(PublishStatus.NotPublished, result.Platform!.Status);
        Assert.True(result.Platform.CanPublish);
        Assert.False(result.Platform.CanDeletePublication);
        Assert.False(deleter.Called);
        Assert.False(repository.DeletePublishedCalled);
    }

    [Fact]
    public async Task DeletePublication_PublishingRow_ReturnsPublishInProgress()
    {
        var handler = CreateHandler(publication: Publication(PublishStatus.Publishing));

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.PublishInProgress, result.Status);
    }

    [Fact]
    public async Task DeletePublication_PastPublishedRow_ReturnsPastStart()
    {
        var handler = CreateHandler(
            calendarEvent: Event(PastStart),
            publication: Publication(PublishStatus.Published));

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.PastStart, result.Status);
    }

    [Fact]
    public async Task DeletePublication_PublishedRowWithoutExternalId_ReturnsMissingExternalResourceId()
    {
        var handler = CreateHandler(
            publication: Publication(PublishStatus.Published, externalResourceId: null));

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.MissingExternalResourceId, result.Status);
    }

    [Fact]
    public async Task DeletePublication_TargetMismatch_ReturnsTargetMismatch()
    {
        var handler = CreateHandler(
            publication: Publication(
                PublishStatus.Published,
                targetSnapshot: new PublicationTargetSnapshot(
                    PlatformType.YouTube,
                    WordPressSiteUrl: null,
                    YouTubeClientId: "other-client-id")));

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.TargetMismatch, result.Status);
    }

    [Fact]
    public async Task DeletePublication_NoProviderDeleter_ReturnsProviderNotSupported()
    {
        var handler = CreateHandler(hasDeleter: false);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.ProviderNotSupported, result.Status);
    }

    [Fact]
    public async Task DeletePublication_ProviderStateConflict_ReturnsProviderStateConflict()
    {
        var repository = new FakePublicationRepository();
        var handler = CreateHandler(
            repository: repository,
            deleter: new FakePublicationDeleter
            {
                Result = PublicationDeleteResult.StateConflict
            });

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.ProviderStateConflict, result.Status);
        Assert.False(repository.DeletePublishedCalled);
    }

    [Fact]
    public async Task DeletePublication_ProviderFailure_ReturnsProviderFailed()
    {
        var handler = CreateHandler(
            deleter: new FakePublicationDeleter
            {
                Result = PublicationDeleteResult.Failed
            });

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.ProviderFailed, result.Status);
    }

    [Fact]
    public async Task DeletePublication_ProviderAlreadyGoneDeletesRow_ReturnsNotPublishedRow()
    {
        var repository = new FakePublicationRepository();
        var deleter = new FakePublicationDeleter
        {
            Result = PublicationDeleteResult.AlreadyGone
        };
        var handler = CreateHandler(repository: repository, deleter: deleter);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.Deleted, result.Status);
        Assert.Equal(ExternalResourceId, repository.DeletedExternalResourceId);
        Assert.Same(Settings, deleter.Request!.PublishSettings);
        Assert.Equal(PublishStatus.NotPublished, result.Platform!.Status);
        Assert.True(result.Platform.CanPublish);
        Assert.False(result.Platform.CanDeletePublication);
    }

    [Fact]
    public async Task DeletePublication_RowChangedAfterProviderCleanup_ReturnsRowChanged()
    {
        var handler = CreateHandler(
            repository: new FakePublicationRepository
            {
                DeletePublishedResult = DeletePublishedResult.Changed
            });

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.RowChanged, result.Status);
    }

    private static Task<DeletePublicationResult> Handle(DeletePublicationHandler handler) =>
        handler.HandleAsync(
            new DeletePublicationCommand(CalendarEventId, PlatformId),
            CancellationToken.None);

    private static DeletePublicationHandler CreateHandler(
        CalendarEventView? calendarEvent = null,
        PlatformView? platform = null,
        PlatformPublication? publication = null,
        FakePublicationRepository? repository = null,
        IPlatformPublicationDeleter? deleter = null,
        bool hasCalendarEvent = true,
        bool hasPlatform = true,
        bool hasPublication = true,
        bool hasDeleter = true) =>
        new(
            new FakeCalendarEventReader(hasCalendarEvent ? calendarEvent ?? Event(FutureStart) : null),
            new FakePlatformReader(hasPlatform ? platform ?? Platform() : null),
            new FakePublicationReader(
                hasPublication ? publication ?? Publication(PublishStatus.Published) : null),
            repository ?? new FakePublicationRepository(),
            new FakePublicationDeleterSelector(
                hasDeleter ? deleter ?? new FakePublicationDeleter() : null),
            new FixedTimeProvider(Now),
            NullLogger<DeletePublicationHandler>.Instance);

    private static CalendarEventView Event(DateTimeOffset startUtc) =>
        new(
            CalendarEventId,
            new ScheduledStart(startUtc.UtcDateTime, "UTC"),
            startUtc,
            [new LocalizedDescription("en", "English title", null)]);

    private static PlatformView Platform() =>
        new(PlatformId, "Main YouTube channel", PlatformType.YouTube, Settings);

    private static PlatformPublication Publication(
        PublishStatus status,
        string? externalResourceId = ExternalResourceId,
        DateTimeOffset? platformDeletedUtc = null,
        PublicationTargetSnapshot? targetSnapshot = null) =>
        new(
            CalendarEventId,
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            status,
            externalResourceId,
            new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero),
            platformDeletedUtc,
            new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero),
            targetSnapshot ?? new PublicationTargetSnapshot(
                PlatformType.YouTube,
                WordPressSiteUrl: null,
                YouTubeClientId: "client-id"));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeCalendarEventReader(CalendarEventView? calendarEvent)
        : ICalendarEventReader
    {
        public Task<IReadOnlyList<CalendarEventView>> ListAsync(
            CalendarEventMonthCriteria? criteria,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CalendarEventView?> GetByIdAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            Task.FromResult<CalendarEventView?>(calendarEvent);
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

    private sealed class FakePublicationReader(PlatformPublication? publication)
        : IPlatformPublicationReader
    {
        public Task<IReadOnlyList<PlatformPublication>> ListByEventAsync(
            string calendarEventId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PlatformPublication?> GetAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            Task.FromResult(publication);

        public Task<IReadOnlyList<PlatformPublication>> ListPublishingByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePublicationRepository : IPlatformPublicationRepository
    {
        public DeletePublishedResult DeletePublishedResult { get; init; } =
            DeletePublishedResult.Deleted;

        public bool DeletePublishedCalled { get; private set; }

        public string? DeletedExternalResourceId { get; private set; }

        public Task<StartPublicationResult> StartPublishingAsync(
            PlatformPublicationAttempt attempt,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task ReleasePublishingAsync(
            string calendarEventId,
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DateTimeOffset?> MarkPublishedAsync(
            string calendarEventId,
            string platformId,
            string externalResourceId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<DeletePublishedResult> DeletePublishedAsync(
            string calendarEventId,
            string platformId,
            string externalResourceId,
            CancellationToken cancellationToken)
        {
            DeletePublishedCalled = true;
            DeletedExternalResourceId = externalResourceId;

            return Task.FromResult(DeletePublishedResult);
        }

        public Task<int> OrphanPublishedByPlatformAsync(
            string platformId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePublicationDeleterSelector(IPlatformPublicationDeleter? deleter)
        : IPublicationDeleterSelector
    {
        public IPlatformPublicationDeleter? Find(PlatformType type) => deleter;
    }

    private sealed class FakePublicationDeleter : IPlatformPublicationDeleter
    {
        public PlatformType Type => PlatformType.YouTube;

        public PublicationDeleteResult Result { get; init; } = PublicationDeleteResult.Deleted;

        public bool Called { get; private set; }

        public PublicationDeleteRequest? Request { get; private set; }

        public Task<PublicationDeleteResult> DeleteAsync(
            PublicationDeleteRequest request,
            CancellationToken cancellationToken)
        {
            Called = true;
            Request = request;

            return Task.FromResult(Result);
        }
    }
}
