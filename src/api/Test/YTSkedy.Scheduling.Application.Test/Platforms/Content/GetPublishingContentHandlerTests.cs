using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Content;
using YTSkedy.Scheduling.Application.Platforms.Publications;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class GetPublishingContentHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";
    private const string YouTubePlatformId = "6ab4a32f3f344de1a7c3a9f4a2f94918";
    private readonly Mock<ICalendarEventReader> _calendarEvents = new();
    private readonly Mock<IPlatformReader> _platforms = new();
    private readonly Mock<IPlatformPublicationReader> _publications = new();
    private readonly Mock<ITemplateReader> _templates = new();
    private readonly GetPublishingContentHandler _handler;

    public GetPublishingContentHandlerTests()
    {
        _handler = new GetPublishingContentHandler(
            _calendarEvents.Object,
            _platforms.Object,
            _publications.Object,
            new PublishingContentRenderer(_templates.Object));
    }

    [Fact]
    public async Task HandleAsync_ActiveNotPublishedRow_ReturnsPreview()
    {
        var handler = CreateHandler(Event(), Platform(), publication: null);

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentType.Preview, result.Type);
        Assert.Equal("English title", result.Content!.Title);
        Assert.Equal("English description", result.Content.Description);
    }

    [Fact]
    public async Task HandleAsync_TemplatePreview_RendersTemplateContent()
    {
        var handler = CreateHandler(
            Event(),
            Platform(),
            publication: null,
            templates: TemplateReader(
                new TemplateView(
                    "title-template",
                    "Title",
                    TemplateType.YouTube,
                    "{{ text1 }} on {{ shortDateEn }}"),
                new TemplateView(
                    "description-template",
                    "Description",
                    TemplateType.YouTube,
                    "Details: {{ text2 }}")));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentType.Preview, result.Type);
        Assert.Equal("English title on 2026-06-25", result.Content!.Title);
        Assert.Equal("Details: English description", result.Content.Description);
    }

    [Fact]
    public async Task HandleAsync_TemplateReferenceKeyToken_ReturnsPreviewWithPublishedExternalResourceId()
    {
        var wordpressPlatform = Platform(
            type: PlatformType.WordPress,
            settings: ApplicationTestData.WordPressSettings("draft"),
            publishingContent: new PublishingContent(
                "wordpress-title-template",
                "wordpress-description-template"));
        var youtubePlatform = Platform(
            platformId: YouTubePlatformId,
            referenceKey: "privateYouTube");
        var handler = CreateHandler(
            Event(),
            wordpressPlatform,
            publication: null,
            templates: TemplateReader(
                new TemplateView(
                    "wordpress-title-template",
                    "WordPress title",
                    TemplateType.WordPress,
                    "{{ text1 }}"),
                new TemplateView(
                    "wordpress-description-template",
                    "WordPress description",
                    TemplateType.WordPress,
                    "YouTube BroadcastId: {{ privateYouTube }}")),
            activePlatforms: [wordpressPlatform, youtubePlatform],
            publicationRows:
            [
                Publication(
                    PublishStatus.Published,
                    contentSnapshot: null,
                    platformId: YouTubePlatformId,
                    externalResourceId: "yt-broadcast-id")
            ]);

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentType.Preview, result.Type);
        Assert.Equal("YouTube BroadcastId: yt-broadcast-id", result.Content!.Description);
    }

    [Fact]
    public async Task HandleAsync_PublishingRowWithSnapshot_ReturnsSnapshot()
    {
        var handler = CreateHandler(
            Event(),
            Platform(),
            Publication(PublishStatus.Publishing, new ContentSnapshot("Stored title", null)));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentType.Snapshot, result.Type);
        Assert.Equal("Stored title", result.Content!.Title);
        Assert.Null(result.Content.Description);
    }

    [Fact]
    public async Task HandleAsync_FailedRowWithSnapshot_ReturnsSnapshot()
    {
        var handler = CreateHandler(
            Event(),
            Platform(),
            Publication(PublishStatus.Failed, new ContentSnapshot("Stored title", null)));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentType.Snapshot, result.Type);
        Assert.Equal("Stored title", result.Content!.Title);
    }

    [Fact]
    public async Task HandleAsync_OrphanPublishedRowWithSnapshot_ReturnsSnapshot()
    {
        var handler = CreateHandler(
            Event(),
            platform: null,
            Publication(
                PublishStatus.Published,
                new ContentSnapshot("Stored title", "Stored description"),
                platformDeletedUtc: new DateTimeOffset(2026, 6, 23, 9, 0, 0, TimeSpan.Zero)));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentType.Snapshot, result.Type);
        Assert.Equal("Stored title", result.Content!.Title);
        Assert.Equal("Stored description", result.Content.Description);
    }

    [Fact]
    public async Task HandleAsync_MissingEvent_ReturnsCalendarEventNotFound()
    {
        var handler = CreateHandler(calendarEvent: null, Platform(), publication: null);

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.CalendarEventNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_ActivePreviewMissingPlatform_ReturnsPlatformNotFound()
    {
        var handler = CreateHandler(Event(), platform: null, publication: null);

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.PlatformNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_PublishedRowWithoutSnapshot_ReturnsPreviewUnavailable()
    {
        var handler = CreateHandler(
            Event(),
            Platform(),
            Publication(PublishStatus.Published, contentSnapshot: null));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.PreviewUnavailable, result.Status);
    }

    [Fact]
    public async Task HandleAsync_MissingTemplate_ReturnsTemplateNotFound()
    {
        var handler = CreateHandler(
            Event(),
            Platform(new PublishingContent("missing-template", "description-template")),
            publication: null);

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.TemplateNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_EmptyRenderedTitle_ReturnsEmptyTitle()
    {
        var handler = CreateHandler(
            Event(description: null),
            Platform(),
            publication: null,
            templates: TemplateReader(
                new TemplateView(
                    "title-template",
                    "Title",
                    TemplateType.YouTube,
                    "{{ text2 }}"),
                new TemplateView(
                    "description-template",
                    "Description",
                    TemplateType.YouTube,
                    "Description")));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.EmptyTitle, result.Status);
    }

    [Fact]
    public async Task HandleAsync_UnresolvedPlaceholder_RemainsVisibleInPreview()
    {
        var handler = CreateHandler(
            Event(),
            Platform(),
            publication: null,
            templates: TemplateReader(
                new TemplateView(
                    "title-template",
                    "Title",
                    TemplateType.YouTube,
                    "{{ unknownToken }}"),
                new TemplateView(
                    "description-template",
                    "Description",
                    TemplateType.YouTube,
                    "Description")));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentType.Preview, result.Type);
        Assert.Equal("{{ unknownToken }}", result.Content!.Title);
    }

    [Fact]
    public async Task HandleAsync_NullQuery_Throws()
    {
        var handler = CreateHandler(Event(), Platform(), publication: null);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!, CancellationToken.None));
    }

    private static Task<GetPublishingContentResult> Handle(GetPublishingContentHandler handler) =>
        handler.HandleAsync(
            new GetPublishingContentQuery(CalendarEventId, PlatformId),
            CancellationToken.None);

    private GetPublishingContentHandler CreateHandler(
        CalendarEventView? calendarEvent,
        PlatformView? platform,
        PlatformPublication? publication,
        Mock<ITemplateReader>? templates = null,
        IReadOnlyList<PlatformView>? activePlatforms = null,
        IReadOnlyList<PlatformPublication>? publicationRows = null)
    {
        _calendarEvents
            .Setup(candidate => candidate.GetByIdAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(calendarEvent);
        _platforms
            .Setup(candidate => candidate.GetAsync(PlatformId, CancellationToken.None))
            .ReturnsAsync(platform);
        _platforms
            .Setup(candidate => candidate.ListAsync(null, CancellationToken.None))
            .ReturnsAsync(activePlatforms ?? (platform is null ? [] : [platform]));
        _publications
            .Setup(candidate => candidate.GetAsync(
                CalendarEventId,
                PlatformId,
                CancellationToken.None))
            .ReturnsAsync(publication);
        _publications
            .Setup(candidate => candidate.ListByEventAsync(
                CalendarEventId,
                CancellationToken.None))
            .ReturnsAsync(publicationRows ?? (publication is null ? [] : [publication]));

        if (templates is null)
        {
            RequiredTemplateReader();
        }

        return _handler;
    }

    private Mock<ITemplateReader> TemplateReader(params TemplateView[] templates)
    {
        foreach (var template in templates)
        {
            _templates
                .Setup(candidate => candidate.GetAsync(
                    template.Type,
                    template.Id,
                    CancellationToken.None))
                .ReturnsAsync(template);
        }

        return _templates;
    }

    private Mock<ITemplateReader> RequiredTemplateReader() =>
        TemplateReader(ApplicationTestData.RequiredTemplates().ToArray());

    private static CalendarEventView Event(string? description = "English description") =>
        ApplicationTestData.CalendarEvent(
            calendarEventId: CalendarEventId,
            start: new ScheduledStart(new DateTime(2026, 6, 25, 10, 0, 0), "America/Vancouver"),
            scheduledStartUtc: new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero),
            text: Text(description));

    private static EventTextSnapshot Text(string? description = "English description") =>
        new(
            [
                new EventTextField("Title", EventTextType.ShortText, 50) { FieldKey = "text1" },
                new EventTextField("Description", EventTextType.LongText, 2500) { FieldKey = "text2" }
            ],
            [
                new EventTextValue("text1", "English title"),
                new EventTextValue("text2", description ?? string.Empty)
            ]);

    private static PlatformView Platform(
        PublishingContent? publishingContent = null,
        string platformId = PlatformId,
        string? referenceKey = null,
        PlatformType type = PlatformType.YouTube,
        PublishSettings? settings = null) =>
        ApplicationTestData.Platform(
            platformId: platformId,
            referenceKey: referenceKey,
            type: type,
            publishSettings: settings,
            publishingContent: publishingContent ?? ApplicationTestData.PublishingContent());

    private static PlatformPublication Publication(
        PublishStatus status,
        ContentSnapshot? contentSnapshot,
        DateTimeOffset? platformDeletedUtc = null,
        string platformId = PlatformId,
        string? externalResourceId = null) =>
        ApplicationTestData.Publication(
            status,
            calendarEventId: CalendarEventId,
            platformId: platformId,
            externalResourceId: externalResourceId,
            platformDeletedUtc: platformDeletedUtc,
            contentSnapshot: contentSnapshot);
}
