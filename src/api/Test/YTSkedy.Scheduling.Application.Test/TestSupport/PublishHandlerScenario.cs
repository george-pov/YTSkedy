using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
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
        IThumbnailPublisher? thumbnailPublisher = null) =>
        new(
            new FakeCalendarEventReader(getResult: calendarEvent),
            new FakeThumbnailReader(thumbnail),
            new FakeThumbnailStore(thumbnailContent),
            new FakePlatformReader(
                platforms: activePlatforms ?? (platform is null ? [] : [platform]),
                getResult: platform),
            new FakePlatformPublicationReader(
                publicationRows ?? (existing is null ? [] : [existing])),
            repository ?? new PublishFakePublicationRepository(),
            new PublishPublisherSelector(publisher),
            new PublishThumbnailPublisherSelector(thumbnailPublisher),
            new PublishingContentRenderer(templates ?? DefaultTemplateReader()),
            new FixedTimeProvider(Now),
            NullLogger<PublishHandler>.Instance);

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

    private sealed class PublishPublisherSelector(IPlatformPublisher? publisher)
        : IPlatformPublisherSelector
    {
        public IPlatformPublisher? Find(PlatformType type) => publisher;
    }

    private sealed class PublishThumbnailPublisherSelector(
        IThumbnailPublisher? publisher) : IThumbnailPublisherSelector
    {
        public IThumbnailPublisher? Find(PlatformType type) => publisher;
    }
}

internal sealed class PublishFakePublicationRepository : IPlatformPublicationRepository
{
    public StartPublicationResult StartResult { get; init; } = StartPublicationResult.Started;

    public DateTimeOffset? MarkPublishedResult { get; init; } =
        new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);

    public bool Started { get; private set; }

    public bool ReleaseCalled { get; private set; }

    public bool MarkPublishedCalled { get; private set; }

    public bool MarkThumbnailAppliedCalled { get; private set; }

    public bool MarkThumbnailFailedCalled { get; private set; }

    public string? MarkedExternalResourceId { get; private set; }

    public PlatformPublicationAttempt? StartedAttempt { get; private set; }

    public Task<StartPublicationResult> StartPublishingAsync(
        PlatformPublicationAttempt attempt,
        CancellationToken cancellationToken)
    {
        Started = StartResult == StartPublicationResult.Started;
        StartedAttempt = attempt;

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

    public Task<DeletePublishedResult> DeletePublishedAsync(
        string calendarEventId,
        string platformId,
        string externalResourceId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<int> OrphanPublishedByPlatformAsync(
        string platformId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

internal sealed class PublishFakePublisher : IPlatformPublisher
{
    private readonly PlatformType type;
    private readonly PlatformPublishResult? result;

    public PublishFakePublisher(
        PlatformType type = PlatformType.YouTube,
        PlatformPublishResult? result = null)
    {
        this.type = type;
        this.result = result;
    }

    public PlatformPublishResult? Result { get; init; }

    public Exception? Throws { get; init; }

    public PlatformPublishRequest? Request { get; private set; }

    public PlatformType Type => type;

    public Task<PlatformPublishResult> PublishAsync(
        PlatformPublishRequest request,
        CancellationToken cancellationToken)
    {
        Request = request;

        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult(Result ?? result ?? new PlatformPublishResult("yt-broadcast-id"));
    }
}

internal sealed class PublishFakeThumbnailPublisher : IThumbnailPublisher
{
    private readonly PlatformType type;

    public PublishFakeThumbnailPublisher(PlatformType type = PlatformType.YouTube)
    {
        this.type = type;
    }

    public Exception? Throws { get; init; }

    public ThumbnailPublishRequest? Request { get; private set; }

    public PlatformType Type => type;

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
