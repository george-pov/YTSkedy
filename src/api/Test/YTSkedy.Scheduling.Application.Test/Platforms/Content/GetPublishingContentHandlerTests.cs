using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Content;
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
            templates: new FakeTemplateReader(
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
            templates: new FakeTemplateReader(
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
            templates: new FakeTemplateReader(
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
            templates: new FakeTemplateReader(
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

    private static GetPublishingContentHandler CreateHandler(
        CalendarEventView? calendarEvent,
        PlatformView? platform,
        PlatformPublication? publication,
        ITemplateReader? templates = null,
        IReadOnlyList<PlatformView>? activePlatforms = null,
        IReadOnlyList<PlatformPublication>? publicationRows = null) =>
        new(
            new FakeCalendarEventReader(getResult: calendarEvent),
            new FakePlatformReader(
                platforms: activePlatforms ?? (platform is null ? [] : [platform]),
                getResult: platform),
            new FakePlatformPublicationReader(
                publicationRows ?? (publication is null ? [] : [publication])),
            new PublishingContentRenderer(
                templates ?? ApplicationTestAdapters.DefaultTemplateReader()));

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
