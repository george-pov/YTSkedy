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

internal static class PublishHandlerScenario
{
    public const string CalendarEventId = ApplicationTestData.CalendarEventId;
    public const string PlatformId = ApplicationTestData.PlatformId;
    public const string YouTubePlatformId = ApplicationTestData.YouTubePlatformId;

    public static readonly DateTimeOffset Now = ApplicationTestData.Now;
    public static readonly DateTimeOffset FutureStart = ApplicationTestData.FutureStart;
    public static readonly DateTimeOffset PastStart =
        new(2026, 6, 1, 17, 0, 0, TimeSpan.Zero);

    public static readonly YouTubeSettings YouTubePublishSettings =
        ApplicationTestData.YouTubeSettings();
    public static readonly WordPressSettings WordPressPublishSettings =
        ApplicationTestData.WordPressSettings();

    public static Task<PublishResult> Handle(PublishHandler handler) =>
        handler.HandleAsync(
            new PublishCommand(CalendarEventId, PlatformId),
            CancellationToken.None);

    public static PublishHandler CreateHandler(
        CalendarEventView? calendarEvent,
        PlatformView? platform,
        IPlatformPublisher? publisher,
        PlatformPublication? existing = null,
        PublishFakePublicationRepository? repository = null,
        ITemplateReader? templates = null,
        IReadOnlyList<PlatformView>? activePlatforms = null,
        IReadOnlyList<PlatformPublication>? publicationRows = null,
        Thumbnail? thumbnail = null,
        ThumbnailContent? thumbnailContent = null,
        IThumbnailPublisher? thumbnailPublisher = null,
        FakeCalendarEventPublicationIndexWriter? publicationIndex = null,
        ILogger<PublishHandler>? logger = null,
        ILogger<PublicationIndexUpdater>? publicationIndexLogger = null,
        IPublishExecutionScopeFactory? executionScopes = null)
    {
        var publicationRepository = repository ?? new PublishFakePublicationRepository();
        var publicationIndexWriter = publicationIndex ?? new FakeCalendarEventPublicationIndexWriter();

        return new PublishHandler(
            new FakeCalendarEventReader(getResult: calendarEvent),
            new FakePlatformReader(
                platforms: activePlatforms ?? (platform is null ? [] : [platform]),
                getResult: platform),
            new FakePlatformPublicationReader(
                publicationRows ?? (existing is null ? [] : [existing])),
            publicationRepository,
            new PublicationIndexUpdater(
                publicationIndexWriter,
                publicationIndexLogger ?? NullLogger<PublicationIndexUpdater>.Instance),
            new PlatformTypeAdapterSelector<IPlatformPublisher>(
                publisher is null ? [] : [publisher]),
            new PublicationThumbnailApplier(
                new FakeThumbnailReader(thumbnail),
                new FakeThumbnailStore(thumbnailContent),
                publicationRepository,
                new PlatformTypeAdapterSelector<IThumbnailPublisher>(
                    thumbnailPublisher is null ? [] : [thumbnailPublisher]),
                NullLogger<PublicationThumbnailApplier>.Instance),
            new PublishingContentRenderer(templates ?? DefaultTemplateReader()),
            executionScopes ?? new PublishFakeExecutionScopeFactory(),
            new FixedTimeProvider(Now),
            logger ?? NullLogger<PublishHandler>.Instance);
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

    private static FakeTemplateReader DefaultTemplateReader() =>
        ApplicationTestAdapters.DefaultTemplateReader();

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

internal sealed class PublishFakePublicationRepository :
    IPublicationAttemptWriter,
    IPublicationThumbnailWriter
{
    public StartPublicationResult StartResult { get; init; } = StartPublicationResult.Started;

    public DateTimeOffset? MarkPublishedResult { get; init; } =
        new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);

    public Exception? MarkPublishedThrows { get; init; }

    public bool Started { get; private set; }

    public bool ReleaseCalled { get; private set; }

    public bool MarkPublishedCalled { get; private set; }

    public MarkFailedResult MarkFailedOutcome { get; init; } = MarkFailedResult.Marked;

    public Exception? MarkFailedThrows { get; init; }

    public bool MarkFailedCalled { get; private set; }

    public SaveExternalResourceIdResult SaveExternalResourceIdOutcome { get; init; } =
        SaveExternalResourceIdResult.Saved;

    public Exception? CheckpointThrows { get; init; }

    public RecoverStalePublishingResult RecoverStalePublishingOutcome { get; init; } =
        RecoverStalePublishingResult.Recovered;

    public bool SaveExternalResourceIdCalled { get; private set; }

    public bool RecoverStalePublishingCalled { get; private set; }

    public DateTimeOffset? RecoveredExpectedUpdatedUtc { get; private set; }

    public bool MarkThumbnailAppliedCalled { get; private set; }

    public bool MarkThumbnailFailedCalled { get; private set; }

    public string? MarkedExternalResourceId { get; private set; }

    public string? FailedExternalResourceId { get; private set; }

    public PlatformPublicationAttempt? StartedAttempt { get; private set; }

    public CancellationToken StartToken { get; private set; }

    public CancellationToken CheckpointToken { get; private set; }

    public CancellationToken MarkPublishedToken { get; private set; }

    public CancellationToken MarkFailedToken { get; private set; }

    public Task<StartPublicationResult> StartPublishingAsync(
        PlatformPublicationAttempt attempt,
        CancellationToken cancellationToken)
    {
        Started = StartResult == StartPublicationResult.Started;
        StartedAttempt = attempt;
        StartToken = cancellationToken;

        return Task.FromResult(StartResult);
    }

    public Task ReleasePublishingAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        ReleaseCalled = true;

        return Task.CompletedTask;
    }

    public Task<SaveExternalResourceIdResult> SaveExternalResourceIdAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken)
    {
        SaveExternalResourceIdCalled = true;
        CheckpointToken = cancellationToken;
        if (CheckpointThrows is not null)
        {
            throw CheckpointThrows;
        }

        return Task.FromResult(SaveExternalResourceIdOutcome);
    }

    public Task<RecoverStalePublishingResult> RecoverStalePublishingAsync(
        string calendarEventId,
        string platformId,
        DateTimeOffset expectedUpdatedUtc,
        CancellationToken cancellationToken)
    {
        RecoverStalePublishingCalled = true;
        RecoveredExpectedUpdatedUtc = expectedUpdatedUtc;
        return Task.FromResult(RecoverStalePublishingOutcome);
    }

    public Task<DateTimeOffset?> MarkPublishedAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken)
    {
        MarkPublishedCalled = true;
        MarkPublishedToken = cancellationToken;
        MarkedExternalResourceId = externalResourceId;

        if (MarkPublishedThrows is not null)
        {
            throw MarkPublishedThrows;
        }

        return Task.FromResult(MarkPublishedResult);
    }

    public Task<MarkFailedResult> MarkFailedAsync(
        string calendarEventId,
        string platformId,
        string? externalResourceId,
        CancellationToken cancellationToken)
    {
        MarkFailedCalled = true;
        MarkFailedToken = cancellationToken;
        FailedExternalResourceId = externalResourceId;

        if (MarkFailedThrows is not null)
        {
            throw MarkFailedThrows;
        }

        return Task.FromResult(MarkFailedOutcome);
    }

    public Task<bool> MarkThumbnailAppliedAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        MarkThumbnailAppliedCalled = true;

        return Task.FromResult(true);
    }

    public Task<bool> MarkThumbnailFailedAsync(
        string calendarEventId,
        string platformId,
        CancellationToken cancellationToken)
    {
        MarkThumbnailFailedCalled = true;

        return Task.FromResult(true);
    }

}

internal sealed class PublishFakePublisher : IPlatformPublisher
{
    private readonly PlatformType _type;
    private readonly PlatformPublishResult? _result;

    public PublishFakePublisher(
        PlatformType type = PlatformType.YouTube,
        PlatformPublishResult? result = null)
    {
        _type = type;
        _result = result;
    }

    public PlatformPublishResult? Result { get; init; }

    public Exception? Throws { get; init; }

    public PlatformPublishRequest? Request { get; private set; }

    public Action? OnPublish { get; init; }

    public CancellationToken CancellationToken { get; private set; }

    public PlatformType Type => _type;

    public Task<PlatformPublishResult> PublishAsync(
        PlatformPublishRequest request,
        IPlatformPublishCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        Request = request;
        CancellationToken = cancellationToken;
        OnPublish?.Invoke();

        if (Throws is not null)
        {
            throw Throws;
        }

        var result = Result ?? _result ?? new PlatformPublishResult("yt-broadcast-id");
        return PublishAndCheckpointAsync(result, checkpoint, cancellationToken);
    }

    private static async Task<PlatformPublishResult> PublishAndCheckpointAsync(
        PlatformPublishResult result,
        IPlatformPublishCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        await checkpoint.SaveExternalResourceIdAsync(
            result.ExternalResourceId,
            cancellationToken);
        return result;
    }
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

internal sealed class PublishFakeThumbnailPublisher : IThumbnailPublisher
{
    private readonly PlatformType _type;

    public PublishFakeThumbnailPublisher(PlatformType type = PlatformType.YouTube)
    {
        _type = type;
    }

    public Exception? Throws { get; init; }

    public ThumbnailPublishRequest? Request { get; private set; }

    public PlatformType Type => _type;

    public Task PublishAsync(
        ThumbnailPublishRequest request,
        CancellationToken cancellationToken)
    {
        Request = request;

        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.CompletedTask;
    }
}
