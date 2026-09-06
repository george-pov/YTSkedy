using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.CalendarEvents.Thumbnails;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Content;
using YTSkedy.Scheduling.Application.Platforms.PublicationThumbnails;
using YTSkedy.Scheduling.Application.Platforms.Providers;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.TestSupport;

namespace YTSkedy.Scheduling.Application.Test;

internal sealed class PublishHandlerScenario
{
    public const string CalendarEventId = ApplicationTestData.CalendarEventId;
    public const string PlatformId = ApplicationTestData.PlatformId;
    public const string YouTubePlatformId = ApplicationTestData.YouTubePlatformId;
    public const string ExternalResourceId = "yt-broadcast-id";

    public static readonly DateTimeOffset Now = ApplicationTestData.Now;
    public static readonly DateTimeOffset FutureStart = ApplicationTestData.FutureStart;
    public static readonly DateTimeOffset PastStart =
        new(2026, 6, 1, 17, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset DefaultPublishedUtc =
        new(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);

    public static readonly YouTubeSettings YouTubePublishSettings =
        ApplicationTestData.YouTubeSettings();
    public static readonly WordPressSettings WordPressPublishSettings =
        ApplicationTestData.WordPressSettings();

    public PublishHandlerScenario()
    {
        CalendarEvent = Event(FutureStart);
        SelectedPlatform = Platform();
        ActivePlatforms = [SelectedPlatform];

        foreach (var template in ApplicationTestData.RequiredTemplates())
        {
            Templates
                .Setup(candidate => candidate.GetAsync(
                    template.Type,
                    template.Id,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(template);
        }

        Publisher.SetupGet(candidate => candidate.Type).Returns(PlatformType.YouTube);
        Publisher
            .Setup(candidate => candidate.PublishAsync(
                It.IsAny<PlatformPublishRequest>(),
                It.IsAny<IPlatformPublishCheckpoint>(),
                It.IsAny<CancellationToken>()))
            .Returns<PlatformPublishRequest, IPlatformPublishCheckpoint, CancellationToken>(
                async (_, checkpoint, cancellationToken) =>
                {
                    await checkpoint.SaveExternalResourceIdAsync(
                        ExternalResourceId,
                        cancellationToken);
                    return new PlatformPublishResult(ExternalResourceId);
                });

        PublicationAttempts
            .Setup(candidate => candidate.StartPublishingAsync(
                It.IsAny<PlatformPublicationAttempt>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(StartPublicationResult.Started);
        PublicationAttempts
            .Setup(candidate => candidate.ReleasePublishingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        PublicationAttempts
            .Setup(candidate => candidate.SaveExternalResourceIdAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SaveExternalResourceIdResult.Saved);
        PublicationAttempts
            .Setup(candidate => candidate.RecoverStalePublishingAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RecoverStalePublishingResult.Recovered);
        PublicationAttempts
            .Setup(candidate => candidate.MarkPublishedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(DefaultPublishedUtc);
        PublicationAttempts
            .Setup(candidate => candidate.MarkFailedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<PublicationFailure>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MarkFailedResult.Marked);

        var publicationThumbnails = PublicationAttempts.As<IPublicationThumbnailWriter>();
        publicationThumbnails
            .Setup(candidate => candidate.MarkThumbnailAppliedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        publicationThumbnails
            .Setup(candidate => candidate.MarkThumbnailFailedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        PublicationIndex
            .Setup(candidate => candidate.AddPublishedPlatformAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        PublicationIndex
            .Setup(candidate => candidate.RemovePublishedPlatformAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ThumbnailPublisher
            .SetupGet(candidate => candidate.Type)
            .Returns(PlatformType.YouTube);
        ThumbnailPublisher
            .Setup(candidate => candidate.PublishAsync(
                It.IsAny<ThumbnailPublishRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public Mock<ICalendarEventReader> CalendarEvents { get; } = new();

    public Mock<IPlatformReader> Platforms { get; } = new();

    public Mock<IPlatformPublicationReader> Publications { get; } = new();

    public Mock<ITemplateReader> Templates { get; } = new();

    public Mock<ICalendarEventThumbnailReader> Thumbnails { get; } = new();

    public Mock<IThumbnailStore> ThumbnailStore { get; } = new();

    public Mock<IPlatformPublisher> Publisher { get; } = new();

    public Mock<IPublicationAttemptWriter> PublicationAttempts { get; } = new();

    public Mock<IPublicationIndexWriter> PublicationIndex { get; } = new();

    public Mock<IThumbnailPublisher> ThumbnailPublisher { get; } = new();

    public Mock<ILogger<PublishHandler>> Logger { get; } = new();

    public Mock<ILogger<PublicationIndexUpdater>> PublicationIndexLogger { get; } = new();

    public PublishFakeExecutionScopeFactory ExecutionScopes { get; } = new();

    public IPublicationThumbnailWriter PublicationThumbnails =>
        PublicationAttempts.As<IPublicationThumbnailWriter>().Object;

    public CalendarEventView? CalendarEvent { get; set; }

    public PlatformView? SelectedPlatform { get; set; }

    public PlatformPublication? ExistingPublication { get; set; }

    public IReadOnlyList<PlatformView> ActivePlatforms { get; set; }

    public IReadOnlyList<PlatformPublication> PublicationRows { get; set; } = [];

    public Thumbnail? CalendarEventThumbnail { get; set; }

    public ThumbnailContent? StoredThumbnailContent { get; set; }

    public Task<PublishResult> HandleAsync(CancellationToken cancellationToken = default) =>
        CreateHandler().HandleAsync(
            new PublishCommand(CalendarEventId, PlatformId),
            cancellationToken);

    public PublishHandler CreateHandler()
    {
        CalendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                CalendarEventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CalendarEvent);
        Platforms
            .Setup(candidate => candidate.GetAsync(
                PlatformId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(SelectedPlatform);
        Platforms
            .Setup(candidate => candidate.ListAsync(
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActivePlatforms);
        Publications
            .Setup(candidate => candidate.GetAsync(
                CalendarEventId,
                PlatformId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExistingPublication);
        Publications
            .Setup(candidate => candidate.ListByEventAsync(
                CalendarEventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PublicationRows);
        Thumbnails
            .Setup(candidate => candidate.GetThumbnailAsync(
                CalendarEventId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CalendarEventThumbnail);

        if (CalendarEventThumbnail is not null)
        {
            ThumbnailStore
                .Setup(candidate => candidate.GetAsync(
                    CalendarEventThumbnail.BlobName,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(StoredThumbnailContent);
        }

        return new PublishHandler(
            CalendarEvents.Object,
            Platforms.Object,
            Publications.Object,
            PublicationAttempts.Object,
            new PublicationIndexUpdater(
                PublicationIndex.Object,
                PublicationIndexLogger.Object),
            new PlatformTypeAdapterSelector<IPlatformPublisher>([Publisher.Object]),
            new PublicationThumbnailApplier(
                Thumbnails.Object,
                ThumbnailStore.Object,
                PublicationThumbnails,
                new PlatformTypeAdapterSelector<IThumbnailPublisher>(
                    [ThumbnailPublisher.Object]),
                NullLogger<PublicationThumbnailApplier>.Instance),
            new PublishingContentRenderer(Templates.Object),
            ExecutionScopes,
            new FixedTimeProvider(Now),
            Logger.Object);
    }

    public static CalendarEventView Event(
        DateTimeOffset startUtc,
        EventTextSnapshot? text = null) =>
        ApplicationTestData.CalendarEvent(
            calendarEventId: CalendarEventId,
            scheduledStartUtc: startUtc,
            start: new ScheduledStart(startUtc.UtcDateTime, "UTC"),
            text: text ?? Text());

    public static EventTextSnapshot Text(
        string? title = "English title",
        string? description = "English description") =>
        ApplicationTestData.Text(title, description);

    public static PlatformView Platform() =>
        Platform("Main YouTube channel", PlatformType.YouTube, YouTubePublishSettings);

    public static PlatformView Platform(PublishingContent publishingContent) =>
        Platform(
            "Main YouTube channel",
            PlatformType.YouTube,
            YouTubePublishSettings,
            publishingContent);

    public static PlatformView Platform(
        string name,
        PlatformType type,
        PublishSettings settings,
        PublishingContent? publishingContent = null,
        string? referenceKey = null) =>
        Platform(
            PlatformId,
            name,
            type,
            settings,
            publishingContent,
            referenceKey);

    public static PlatformView Platform(
        string platformId,
        string name,
        PlatformType type,
        PublishSettings settings,
        PublishingContent? publishingContent = null,
        string? referenceKey = null) =>
        ApplicationTestData.Platform(
            platformId: platformId,
            name: name,
            referenceKey: referenceKey,
            type: type,
            publishSettings: settings,
            publishingContent: publishingContent ?? ApplicationTestData.PublishingContent());

    public static PlatformPublication Publication(
        PublishStatus status,
        DateTimeOffset? platformDeletedUtc = null,
        string platformId = PlatformId,
        string? externalResourceId = null,
        ThumbnailPublishStatus? thumbnailStatus = null) =>
        ApplicationTestData.Publication(
            status,
            calendarEventId: CalendarEventId,
            platformId: platformId,
            platformName: "Main YouTube channel",
            externalResourceId: externalResourceId,
            platformDeletedUtc: platformDeletedUtc,
            updatedUtc: Now,
            thumbnailStatus: thumbnailStatus);

    public static Thumbnail Thumbnail() =>
        ApplicationTestData.Thumbnail(
            calendarEventId: CalendarEventId,
            sizeBytes: 11,
            updatedUtc: Now);

    public static ThumbnailContent ThumbnailContent() =>
        ApplicationTestData.ThumbnailContent();
}

internal sealed class PublishFakeExecutionScopeFactory : IPublishExecutionScopeFactory
{
    public PublishFakeExecutionScope Scope { get; } = new();

    public bool CreateCalled { get; private set; }

    public IPublishExecutionScope Create()
    {
        CreateCalled = true;
        return Scope;
    }
}

internal sealed class PublishFakeExecutionScope : IPublishExecutionScope
{
    public CancellationToken OperationToken { get; set; }

    public PublishCancellationSource CancellationSource { get; set; } =
        PublishCancellationSource.Unexpected;

    public Exception? FinalizationThrows { get; set; }

    public CancellationToken FinalizationToken { get; set; }

    public int FinalizationCalls { get; private set; }

    public PublishCancellationSource ClassifyCancellation() => CancellationSource;

    public async Task<TResult> RunFinalizationAsync<TResult>(
        Func<CancellationToken, Task<TResult>> action)
    {
        FinalizationCalls++;
        if (FinalizationThrows is not null)
        {
            throw FinalizationThrows;
        }

        return await action(FinalizationToken);
    }

    public void Dispose()
    {
    }
}
