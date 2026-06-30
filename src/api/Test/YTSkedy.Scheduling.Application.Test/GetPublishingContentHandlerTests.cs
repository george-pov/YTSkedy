using YTSkedy.Scheduling.Application.CalendarEvents;
using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class GetPublishingContentHandlerTests
{
    private const string CalendarEventId = "f81d4fae7dec11d0a76500a0c91e6bf6";
    private const string PlatformId = "4fb4a32f3f344de1a7c3a9f4a2f94918";

    [Fact]
    public async Task HandleAsync_ActiveNotPublishedRow_ReturnsPreview()
    {
        var handler = CreateHandler(Event(), Platform(), publication: null);

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentKind.Preview, result.Kind);
        Assert.Equal("English title", result.Content!.Title);
        Assert.Equal("English description", result.Content.Description);
    }

    [Fact]
    public async Task HandleAsync_TemplatePreview_RendersTemplateContent()
    {
        var handler = CreateHandler(
            Event(),
            Platform(new PublishingContent("title-template", null)),
            publication: null,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title",
                    TemplateType.YouTube,
                    "{{ title }} on {{ shortDate }}")));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentKind.Preview, result.Kind);
        Assert.Equal("English title on 2026-06-25", result.Content!.Title);
        Assert.Equal("English description", result.Content.Description);
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
        Assert.Equal(PublishingContentKind.Snapshot, result.Kind);
        Assert.Equal("Stored title", result.Content!.Title);
        Assert.Null(result.Content.Description);
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
        Assert.Equal(PublishingContentKind.Snapshot, result.Kind);
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
            Platform(new PublishingContent("missing-template", null)),
            publication: null);

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.TemplateNotFound, result.Status);
    }

    [Fact]
    public async Task HandleAsync_EmptyRenderedTitle_ReturnsEmptyTitle()
    {
        var handler = CreateHandler(
            Event(description: null),
            Platform(new PublishingContent("title-template", null)),
            publication: null,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title",
                    TemplateType.YouTube,
                    "{{ description }}")));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.EmptyTitle, result.Status);
    }

    [Fact]
    public async Task HandleAsync_UnresolvedPlaceholder_RemainsVisibleInPreview()
    {
        var handler = CreateHandler(
            Event(),
            Platform(new PublishingContent("title-template", null)),
            publication: null,
            templates: new FakeTemplateReader(
                new TemplateView(
                    "title-template",
                    "Title",
                    TemplateType.YouTube,
                    "{{ unknownToken }}")));

        var result = await Handle(handler);

        Assert.Equal(GetPublishingContentStatus.Found, result.Status);
        Assert.Equal(PublishingContentKind.Preview, result.Kind);
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
        ITemplateReader? templates = null) =>
        new(
            new FakeCalendarEventReader(calendarEvent),
            new FakePlatformReader(platform),
            new FakePublicationReader(publication),
            new PublishingContentRenderer(templates ?? new FakeTemplateReader()));

    private static CalendarEventView Event(string? description = "English description") =>
        new(
            CalendarEventId,
            new ScheduledStart(new DateTime(2026, 6, 25, 10, 0, 0), "America/Vancouver"),
            new DateTimeOffset(2026, 6, 25, 17, 0, 0, TimeSpan.Zero),
            [new LocalizedDescription("en", "English title", description)]);

    private static PlatformView Platform(PublishingContent? publishingContent = null) =>
        new(
            PlatformId,
            "Main YouTube channel",
            null,
            PlatformType.YouTube,
            new YouTubeSettings(
                new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
                "private",
                false),
            publishingContent);

    private static PlatformPublication Publication(
        PublishStatus status,
        ContentSnapshot? contentSnapshot,
        DateTimeOffset? platformDeletedUtc = null) =>
        new(
            CalendarEventId,
            PlatformId,
            "Main YouTube channel",
            PlatformType.YouTube,
            status,
            ExternalResourceId: null,
            PublishedUtc: null,
            PlatformDeletedUtc: platformDeletedUtc,
            UpdatedUtc: new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero),
            TargetSnapshot: null,
            ContentSnapshot: contentSnapshot);

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
}
