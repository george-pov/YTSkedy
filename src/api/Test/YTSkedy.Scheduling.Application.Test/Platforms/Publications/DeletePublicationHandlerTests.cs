using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.TestSupport;

namespace YTSkedy.Scheduling.Application.Test;

public class DeletePublicationHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string ExternalResourceId = "yt-broadcast-id";

    private static readonly DateTimeOffset Now = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStart = new(2026, 6, 15, 17, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastStart = Now;
    private static readonly YouTubeSettings Settings = ApplicationTestData.YouTubeSettings();
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<IPlatformReader> _platforms = new();
    private readonly Mock<IPlatformPublicationReader> _publications = new();
    private readonly Mock<IPublicationCleanupWriter> _repository = new();
    private readonly Mock<IPlatformPublicationDeleter> _deleter = new();
    private readonly Mock<IPublicationIndexWriter> _publicationIndex = new();
    private readonly Mock<ILogger<PublicationIndexUpdater>> _publicationIndexLogger = new();

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
        var repository = PublicationRepository();
        var deleter = PublicationDeleter();
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
        deleter.Verify(candidate => candidate.DeleteAsync(
            It.IsAny<PublicationDeleteRequest>(),
            It.IsAny<CancellationToken>()), Times.Never());
        repository.Verify(candidate => candidate.DeletePublishedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
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
        var repository = PublicationRepository();
        var handler = CreateHandler(
            repository: repository,
            deleter: PublicationDeleter(PublicationDeleteResult.StateConflict));

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.ProviderStateConflict, result.Status);
        repository.Verify(candidate => candidate.DeletePublishedAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DeletePublication_ProviderFailure_ReturnsProviderFailed()
    {
        var publicationIndex = PublicationIndex();
        var handler = CreateHandler(
            deleter: PublicationDeleter(PublicationDeleteResult.Failed),
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.ProviderFailed, result.Status);
        publicationIndex.Verify(candidate => candidate.RemovePublishedPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DeletePublication_ProviderAlreadyGoneDeletesRow_ReturnsNotPublishedRow()
    {
        var repository = PublicationRepository();
        var deleter = PublicationDeleter(PublicationDeleteResult.AlreadyGone);
        var publicationIndex = PublicationIndex();
        var handler = CreateHandler(
            repository: repository,
            deleter: deleter,
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.Deleted, result.Status);
        repository.Verify(candidate => candidate.DeletePublishedAsync(
            CalendarEventId,
            PlatformId,
            ExternalResourceId,
            CancellationToken.None));
        deleter.Verify(candidate => candidate.DeleteAsync(
            It.Is<PublicationDeleteRequest>(request =>
                ReferenceEquals(request.PublishSettings, Settings)),
            CancellationToken.None));
        Assert.Equal(PublishStatus.NotPublished, result.Platform!.Status);
        Assert.True(result.Platform.CanPublish);
        Assert.False(result.Platform.CanDeletePublication);
        publicationIndex.Verify(candidate => candidate.RemovePublishedPlatformAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
    }

    [Fact]
    public async Task DeletePublication_RowAlreadyMissingAfterProviderCleanup_RemovesIndex()
    {
        var repository = PublicationRepository(DeletePublishedResult.NotFound);
        var publicationIndex = PublicationIndex();
        var handler = CreateHandler(
            repository: repository,
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.Deleted, result.Status);
        publicationIndex.Verify(candidate => candidate.RemovePublishedPlatformAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
    }

    [Fact]
    public async Task DeletePublication_RowChangedAfterProviderCleanup_ReturnsRowChanged()
    {
        var publicationIndex = PublicationIndex();
        var handler = CreateHandler(
            repository: PublicationRepository(DeletePublishedResult.Changed),
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.RowChanged, result.Status);
        publicationIndex.Verify(candidate => candidate.RemovePublishedPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task DeletePublication_IndexReturnsFalse_LogsAndReturnsDeleted()
    {
        var publicationIndex = PublicationIndex(removeResult: false);
        var handler = CreateHandler(
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.Deleted, result.Status);
        publicationIndex.Verify(candidate => candidate.RemovePublishedPlatformAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
        var entry = Assert.Single(_publicationIndexLogger.GetLogEntries());
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("RemovePublishedPlatform", entry.Message, StringComparison.Ordinal);
        Assert.Contains(CalendarEventId, entry.Message, StringComparison.Ordinal);
        Assert.Contains(PlatformId, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeletePublication_IndexThrows_LogsAndReturnsDeleted()
    {
        var publicationIndex = PublicationIndex(
            exception: new InvalidOperationException("storage unavailable"));
        var handler = CreateHandler(
            publicationIndex: publicationIndex);

        var result = await Handle(handler);

        Assert.Equal(DeletePublicationStatus.Deleted, result.Status);
        publicationIndex.Verify(candidate => candidate.RemovePublishedPlatformAsync(
            CalendarEventId,
            PlatformId,
            CancellationToken.None));
        var entry = Assert.Single(_publicationIndexLogger.GetLogEntries());
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("RemovePublishedPlatform", entry.Message, StringComparison.Ordinal);
    }

    private static Task<DeletePublicationResult> Handle(DeletePublicationHandler handler) =>
        handler.HandleAsync(
            new DeletePublicationCommand(CalendarEventId, PlatformId),
            CancellationToken.None);

    private DeletePublicationHandler CreateHandler(
        CalendarEventView? calendarEvent = null,
        PlatformView? platform = null,
        PlatformPublication? publication = null,
        Mock<IPublicationCleanupWriter>? repository = null,
        Mock<IPlatformPublicationDeleter>? deleter = null,
        bool hasCalendarEvent = true,
        bool hasPlatform = true,
        bool hasPublication = true,
        bool hasDeleter = true,
        Mock<IPublicationIndexWriter>? publicationIndex = null)
    {
        _calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(hasCalendarEvent ? calendarEvent ?? Event(FutureStart) : null);
        _platforms
            .Setup(candidate => candidate.GetAsync(PlatformId, CancellationToken.None))
            .ReturnsAsync(hasPlatform ? platform ?? Platform() : null);
        _publications
            .Setup(candidate => candidate.GetAsync(
                CalendarEventId,
                PlatformId,
                CancellationToken.None))
            .ReturnsAsync(hasPublication ? publication ?? Publication(PublishStatus.Published) : null);

        if (repository is null)
        {
            PublicationRepository();
        }
        if (deleter is null)
        {
            PublicationDeleter();
        }
        if (publicationIndex is null)
        {
            PublicationIndex();
        }

        return new DeletePublicationHandler(
            _calendarEvents.Object,
            _platforms.Object,
            _publications.Object,
            _repository.Object,
            new PublicationIndexUpdater(
                _publicationIndex.Object,
                _publicationIndexLogger.Object),
            new PlatformTypeAdapterSelector<IPlatformPublicationDeleter>(
                hasDeleter ? [_deleter.Object] : []),
            new FixedTimeProvider(Now),
            NullLogger<DeletePublicationHandler>.Instance);
    }

    private static CalendarEventView Event(DateTimeOffset startUtc) =>
        ApplicationTestData.CalendarEvent(
            calendarEventId: CalendarEventId,
            scheduledStartUtc: startUtc,
            start: new ScheduledStart(startUtc.UtcDateTime, "UTC"));

    private static PlatformView Platform() =>
        ApplicationTestData.Platform(
            platformId: PlatformId,
            name: "Main YouTube channel",
            publishSettings: Settings);

    private static PlatformPublication Publication(
        PublishStatus status,
        string? externalResourceId = ExternalResourceId,
        DateTimeOffset? platformDeletedUtc = null,
        PublicationTargetSnapshot? targetSnapshot = null) =>
        ApplicationTestData.Publication(
            status,
            calendarEventId: CalendarEventId,
            platformId: PlatformId,
            platformName: "Main YouTube channel",
            externalResourceId: externalResourceId,
            publishedUtc: new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero),
            platformDeletedUtc: platformDeletedUtc,
            updatedUtc: new DateTimeOffset(2026, 5, 30, 12, 0, 0, TimeSpan.Zero),
            targetSnapshot: targetSnapshot ?? new PublicationTargetSnapshot(
                PlatformType.YouTube,
                WordPressSiteUrl: null,
                YouTubeClientId: ApplicationTestData.YouTubeClientId));

    private Mock<IPublicationCleanupWriter> PublicationRepository(
        DeletePublishedResult? result = null)
    {
        _repository
            .Setup(candidate => candidate.DeletePublishedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? DeletePublishedResult.Deleted);
        return _repository;
    }

    private Mock<IPlatformPublicationDeleter> PublicationDeleter(
        PublicationDeleteResult? result = null)
    {
        _deleter.SetupGet(candidate => candidate.Type).Returns(PlatformType.YouTube);
        _deleter
            .Setup(candidate => candidate.DeleteAsync(
                It.IsAny<PublicationDeleteRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result ?? PublicationDeleteResult.Deleted);
        return _deleter;
    }

    private Mock<IPublicationIndexWriter> PublicationIndex(
        bool removeResult = true,
        Exception? exception = null)
    {
        var setup = _publicationIndex.Setup(candidate => candidate.RemovePublishedPlatformAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        if (exception is null)
        {
            setup.ReturnsAsync(removeResult);
        }
        else
        {
            setup.ThrowsAsync(exception);
        }

        return _publicationIndex;
    }
}
