using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Application.Platforms.Content;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;
using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishingContentRendererTests
{
    [Fact]
    public void Render_KnownTokens_ReplacesPlaceholders()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ text1 }} on {{ shortDateEn }}",
            "Details: {{ text2 }}",
            Event(),
            null);

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.NotNull(result.Content);
        Assert.Equal("English title on 2026-06-05", result.Content!.Title);
        Assert.Equal("Details: English description", result.Content.Description);
        Assert.False(result.HasUnresolvedPlaceholders);
    }

    [Fact]
    public void Render_WhitespaceAroundTokens_ReplacesPlaceholders()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{    text3    }}",
            null,
            Event(),
            null);

        Assert.Equal("Russian title", result.Content!.Title);
    }

    [Fact]
    public void Render_RepeatedTokens_ReplacesEveryOccurrence()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ text1 }} / {{ text1 }}",
            null,
            Event(),
            null);

        Assert.Equal("English title / English title", result.Content!.Title);
    }

    [Fact]
    public void Render_UnknownWellFormedToken_LeavesPlaceholderAndReportsUnresolved()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ tittle }}",
            "Text {{ unknownToken }}",
            Event(),
            null);

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.Equal("{{ tittle }}", result.Content!.Title);
        Assert.Equal("Text {{ unknownToken }}", result.Content.Description);
        Assert.True(result.HasUnresolvedPlaceholders);
    }

    [Fact]
    public void Render_RuntimeToken_ReplacesReferenceKeyPlaceholder()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ text1 }}",
            "YouTube BroadcastId: {{ privateYouTube }}",
            Event(),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["privateYouTube"] = "yt-broadcast-id"
            });

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.Equal("YouTube BroadcastId: yt-broadcast-id", result.Content!.Description);
        Assert.False(result.HasUnresolvedPlaceholders);
    }

    [Fact]
    public void Render_RuntimeToken_DoesNotOverrideCalendarEventToken()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ text1 }}",
            null,
            Event(),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["text1"] = "yt-broadcast-id"
            });

        Assert.Equal("English title", result.Content!.Title);
    }

    [Fact]
    public void Render_MalformedBraces_LeavesTextAndDoesNotReportUnresolved()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{ text1 } and {{ text1",
            "Text {{ text 1 }}",
            Event(),
            null);

        Assert.Equal("{ text1 } and {{ text1", result.Content!.Title);
        Assert.Equal("Text {{ text 1 }}", result.Content.Description);
        Assert.False(result.HasUnresolvedPlaceholders);
    }

    [Fact]
    public void Render_EmptyRenderedTitle_ReturnsEmptyTitle()
    {
        var renderer = new PublishingContentRenderer();
        var calendarEvent = Event(description: null);

        var result = renderer.Render(
            "{{ text2 }}",
            null,
            calendarEvent,
            null);

        Assert.Equal(RenderContentStatus.EmptyTitle, result.Status);
        Assert.Null(result.Content);
        Assert.False(result.HasUnresolvedPlaceholders);
    }

    [Fact]
    public void Render_EmptyRenderedDescription_NormalizesDescriptionToNull()
    {
        var renderer = new PublishingContentRenderer();
        var calendarEvent = Event(description: null);

        var result = renderer.Render(
            "{{ text1 }}",
            "{{ text2 }}",
            calendarEvent,
            null);

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.Null(result.Content!.Description);
    }

    [Fact]
    public async Task RenderAsync_TemplateIds_RendersTemplateContent()
    {
        var templates = new Mock<ITemplateReader>();
        templates
            .Setup(candidate => candidate.GetAsync(
                TemplateType.YouTube,
                "title-template",
                CancellationToken.None))
            .ReturnsAsync(new TemplateView(
                "title-template",
                "Title",
                TemplateType.YouTube,
                "{{ text1 }} on {{ shortDateEn }}"));
        templates
            .Setup(candidate => candidate.GetAsync(
                TemplateType.YouTube,
                "description-template",
                CancellationToken.None))
            .ReturnsAsync(new TemplateView(
                "description-template",
                "Description",
                TemplateType.YouTube,
                "Details: {{ text2 }}"));
        var renderer = new PublishingContentRenderer(templates.Object);

        var result = await renderer.RenderAsync(
            Platform(new PublishingContent("title-template", "description-template")),
            Event(),
            runtimeTokenValues: null,
            CancellationToken.None);

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.Equal("English title on 2026-06-05", result.Content!.Title);
        Assert.Equal("Details: English description", result.Content.Description);
    }

    [Fact]
    public async Task RenderAsync_MissingTemplate_ReturnsTemplateNotFound()
    {
        var templates = new Mock<ITemplateReader>();
        templates
            .Setup(candidate => candidate.GetAsync(
                TemplateType.YouTube,
                "missing-template",
                CancellationToken.None))
            .ReturnsAsync((TemplateView?)null);
        var renderer = new PublishingContentRenderer(templates.Object);

        var result = await renderer.RenderAsync(
            Platform(new PublishingContent("missing-template", "description-template")),
            Event(),
            runtimeTokenValues: null,
            CancellationToken.None);

        Assert.Equal(RenderContentStatus.TemplateNotFound, result.Status);
        Assert.Null(result.Content);
    }

    private static CalendarEventView Event(string? description = "English description") =>
        new(
            "calendar-event-id",
            new ScheduledStart(new DateTime(2026, 6, 5, 10, 30, 0), "America/Vancouver"),
            new DateTimeOffset(2026, 6, 5, 17, 30, 0, TimeSpan.Zero),
            Text(description));

    private static EventTextSnapshot Text(string? description = "English description") =>
        new(
            [
                new EventTextField("Title", EventTextType.ShortText, 50) { FieldKey = "text1" },
                new EventTextField("Description", EventTextType.LongText, 2500) { FieldKey = "text2" },
                new EventTextField("Russian title", EventTextType.ShortText, 50) { FieldKey = "text3" },
                new EventTextField("Russian description", EventTextType.LongText, 2500) { FieldKey = "text4" }
            ],
            [
                new EventTextValue("text1", "English title"),
                new EventTextValue("text2", description ?? string.Empty),
                new EventTextValue("text3", "Russian title"),
                new EventTextValue("text4", "Russian description")
            ]);

    private static PlatformView Platform(PublishingContent publishingContent) =>
        ApplicationTestData.Platform(
            platformId: "platform-id",
            name: "Main channel",
            publishingContent: publishingContent);
}
