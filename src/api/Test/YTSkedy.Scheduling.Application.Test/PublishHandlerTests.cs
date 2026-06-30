using Microsoft.Extensions.Logging.Abstractions;
using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset FutureStart = new(2026, 6, 25, 17, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset PastStart = new(2026, 6, 1, 17, 0, 0, TimeSpan.Zero);

    private static readonly YouTubeSettings Settings = new(
        new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
        "private",
        false);
    private static readonly WordPressSettings WordPressSettings =
        new("https://blog.example.test/", "publisher", "application-password", "publish");

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsEventNotFound()
    {
        var handler = CreateHandler(calendarEvent: null, platform: Platform(), publisher: new FakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.EventNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_MissingPlatform_ReturnsPlatformNotFound()
    {
        var handler = CreateHandler(Event(FutureStart), platform: null, publisher: new FakePublisher());

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
            new FakePublisher(),
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
            new FakePublisher(),
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
            new FakePublisher(),
            existing: Publication(PublishStatus.Publishing));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PublishInProgress, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PastStart_ReturnsPastStart()
    {
        var handler = CreateHandler(Event(PastStart), Platform(), new FakePublisher());

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PastStart, result.Status);
    }

    [Fact]
    public async Task HandleAsync_NoTitleText_ReturnsInvalidPublishingContent()
    {
        var repository = new FakePublicationRepository();
        var publisher = new FakePublisher();
        var handler = CreateHandler(
            Event(FutureStart, Text(title: null)),
            Platform(),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_BlankTitleText_ReturnsInvalidPublishingContent()
    {
        var repository = new FakePublicationRepository();
        var publisher = new FakePublisher();
        var handler = CreateHandler(
            Event(FutureStart, Text(title: "   ", description: "description")),
            Platform(),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_TemplateContent_RendersBeforePublishing()
    {
        var repository = new FakePublicationRepository();
        var publisher = new FakePublisher { Result = new PlatformPublishResult("yt-broadcast-id") };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(publishingContent: new PublishingContent(
                "title-template",
                "description-template")),
            publisher,
            repository: repository,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title template",
                    TemplateType.YouTube,
                    "{{ title }} on {{ shortDate }}"),
                new TemplateView(
                    "description-template",
                    "Description template",
                    TemplateType.YouTube,
                    "Details: {{ description }}")));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.Equal("English title on 2026-06-25", publisher.Request!.Title);
        Assert.Equal("Details: English description", publisher.Request.Description);
        Assert.Equal("English title on 2026-06-25", repository.StartedAttempt!.ContentSnapshot.Title);
        Assert.Equal("Details: English description", repository.StartedAttempt.ContentSnapshot.Description);
    }

    [Fact]
    public async Task HandleAsync_MissingTemplate_ReturnsInvalidPublishingContent()
    {
        var repository = new FakePublicationRepository();
        var publisher = new FakePublisher();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(publishingContent: new PublishingContent("missing-template", null)),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_EmptyRenderedTitle_ReturnsInvalidPublishingContent()
    {
        var repository = new FakePublicationRepository();
        var publisher = new FakePublisher();
        var handler = CreateHandler(
            Event(FutureStart, Text(description: string.Empty)),
            Platform(publishingContent: new PublishingContent("title-template", null)),
            publisher,
            repository: repository,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title template",
                    TemplateType.YouTube,
                    "{{ description }}")));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_UnresolvedToken_ReturnsInvalidPublishingContent()
    {
        var repository = new FakePublicationRepository();
        var publisher = new FakePublisher();
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(publishingContent: new PublishingContent("title-template", null)),
            publisher,
            repository: repository,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title template",
                    TemplateType.YouTube,
                    "{{ unknownToken }}")));

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.InvalidPublishingContent, result.Status);
        Assert.False(repository.Started);
        Assert.Null(publisher.Request);
    }

    [Fact]
    public async Task HandleAsync_AttemptConflict_ReturnsPublishInProgress()
    {
        var repository = new FakePublicationRepository { StartResult = StartPublicationResult.Conflict };
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(),
            new FakePublisher(),
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.PublishInProgress, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ProviderFailure_ReleasesAttemptAndReturnsProviderFailed()
    {
        var repository = new FakePublicationRepository();
        var publisher = new FakePublisher { Throws = new PlatformPublishException("provider down") };
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher, repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.ProviderFailed, result.Status);
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

        Assert.Equal(PublishResultStatus.FinalizeFailed, result.Status);
        Assert.False(repository.ReleaseCalled);
    }

    [Fact]
    public async Task HandleAsync_Success_StartsPublishesFinalizesAndReturnsPublished()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);
        var repository = new FakePublicationRepository { MarkPublishedResult = publishedUtc };
        var publisher = new FakePublisher { Result = new PlatformPublishResult("yt-broadcast-id") };
        var handler = CreateHandler(Event(FutureStart), Platform(), publisher, repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal(PlatformId, result.Platform!.PlatformId);
        Assert.Equal("Main YouTube channel", result.Platform.PlatformName);
        Assert.Equal(PlatformType.YouTube, result.Platform.PlatformType);
        Assert.Equal(PublishStatus.Published, result.Platform.Status);
        Assert.Equal("yt-broadcast-id", result.Platform.ExternalResourceId);
        Assert.Equal(publishedUtc, result.Platform.PublishedUtc);
        Assert.Null(result.Platform.PlatformDeletedUtc);
        Assert.False(result.Platform.CanPublish);
        Assert.True(result.Platform.CanDeletePublication);
        Assert.True(result.Platform.CanPreviewPublishingContent);

        Assert.True(repository.Started);
        Assert.Equal("yt-broadcast-id", repository.MarkedExternalResourceId);
        Assert.False(repository.ReleaseCalled);
        Assert.Equal("English title", repository.StartedAttempt!.ContentSnapshot.Title);
        Assert.Equal("English description", repository.StartedAttempt.ContentSnapshot.Description);

        // The provider receives the English content and the stored future start.
        Assert.Equal("English title", publisher.Request!.Title);
        Assert.Equal("English description", publisher.Request.Description);
        Assert.Equal(FutureStart, publisher.Request.ScheduledStartUtc);
        Assert.Same(Settings, publisher.Request.PublishSettings);
    }

    [Fact]
    public async Task HandleAsync_WordPressSuccess_ReturnsWordPressPlatformAndPostId()
    {
        var publishedUtc = new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);
        var repository = new FakePublicationRepository { MarkPublishedResult = publishedUtc };
        var publisher = new FakePublisher(
            PlatformType.WordPress,
            new PlatformPublishResult("123"));
        var handler = CreateHandler(
            Event(FutureStart),
            Platform(
                "Company blog",
                PlatformType.WordPress,
                WordPressSettings),
            publisher,
            repository: repository);

        var result = await Handle(handler);

        Assert.Equal(PublishResultStatus.Published, result.Status);
        Assert.NotNull(result.Platform);
        Assert.Equal("Company blog", result.Platform!.PlatformName);
        Assert.Equal(PlatformType.WordPress, result.Platform.PlatformType);
        Assert.Equal(publishedUtc, result.Platform.PublishedUtc);
        Assert.Equal("123", result.Platform.ExternalResourceId);
        Assert.False(result.Platform.CanPublish);
        Assert.True(result.Platform.CanDeletePublication);
        Assert.True(result.Platform.CanPreviewPublishingContent);

        Assert.Equal("123", repository.MarkedExternalResourceId);
        Assert.Same(WordPressSettings, publisher.Request!.PublishSettings);
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
        FakePublicationRepository? repository = null,
        ITemplateReader? templates = null) =>
        new(
            new FakeCalendarEventReader(calendarEvent),
            new FakePlatformReader(platform),
            new FakePublicationReader(existing),
            repository ?? new FakePublicationRepository(),
            new FakeSelector(publisher),
            new PublishingContentRenderer(templates ?? new FakeTemplateReader()),
            new FixedTimeProvider(Now),
            NullLogger<PublishHandler>.Instance);

    private static CalendarEventView Event(
        DateTimeOffset startUtc,
        EventTextSnapshot? text = null) =>
        new(
            CalendarEventId,
            new ScheduledStart(startUtc.UtcDateTime, "UTC"),
            startUtc,
            text ?? Text());

    private static EventTextSnapshot Text(
        string? title = "English title",
        string? description = "English description")
    {
        var values = new List<EventTextValue>();
        if (title is not null)
        {
            values.Add(new EventTextValue("text1", title));
        }

        values.Add(new EventTextValue("text2", description ?? string.Empty));

        return new EventTextSnapshot(
            [
                new EventTextField("text1", "Title", EventTextType.ShortText, 50),
                new EventTextField("text2", "Description", EventTextType.LongText, 2500)
            ],
            values);
    }

    private static PlatformView Platform() =>
        Platform("Main YouTube channel", PlatformType.YouTube, Settings);

    private static PlatformView Platform(PublishingContent publishingContent) =>
        Platform("Main YouTube channel", PlatformType.YouTube, Settings, publishingContent);

    private static PlatformView Platform(
        string name,
        PlatformType type,
        PublishSettings settings,
        PublishingContent? publishingContent = null) =>
        new(PlatformId, name, null, type, settings, publishingContent);

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
        public StartPublicationResult StartResult { get; init; } = StartPublicationResult.Started;

        public DateTimeOffset? MarkPublishedResult { get; init; } =
            new DateTimeOffset(2026, 6, 22, 12, 0, 5, TimeSpan.Zero);

        public bool Started { get; private set; }

        public bool ReleaseCalled { get; private set; }

        public bool MarkPublishedCalled { get; private set; }

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

    private sealed class FakeSelector(IPlatformPublisher? publisher) : IPlatformPublisherSelector
    {
        public IPlatformPublisher? Find(PlatformType type) => publisher;
    }

    private sealed class FakeTemplateReader(params TemplateView[] templates) : ITemplateReader
    {
        public Task<TemplateView?> GetAsync(
            TemplateType type,
            string templateId,
            CancellationToken cancellationToken)
        {
            var template = templates.FirstOrDefault(candidate =>
                candidate.Type == type &&
                string.Equals(candidate.Id, templateId, StringComparison.Ordinal));

            return Task.FromResult(template);
        }

        public Task<IReadOnlyList<TemplateView>> ListAsync(
            TemplateType? type,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class FakePublisher : IPlatformPublisher
    {
        private readonly PlatformType type;
        private readonly PlatformPublishResult? result;

        public FakePublisher(
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
}
