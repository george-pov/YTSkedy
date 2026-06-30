using YTSkedy.Scheduling.Application.Platforms;
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
            Event());

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
            Event());

        Assert.Equal("Russian title", result.Content!.Title);
    }

    [Fact]
    public void Render_RepeatedTokens_ReplacesEveryOccurrence()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ text1 }} / {{ text1 }}",
            null,
            Event());

        Assert.Equal("English title / English title", result.Content!.Title);
    }

    [Fact]
    public void Render_UnknownWellFormedToken_LeavesPlaceholderAndReportsUnresolved()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ tittle }}",
            "Text {{ unknownToken }}",
            Event());

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.Equal("{{ tittle }}", result.Content!.Title);
        Assert.Equal("Text {{ unknownToken }}", result.Content.Description);
        Assert.True(result.HasUnresolvedPlaceholders);
    }

    [Fact]
    public void Render_MalformedBraces_LeavesTextAndDoesNotReportUnresolved()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{ text1 } and {{ text1",
            "Text {{ text 1 }}",
            Event());

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
            calendarEvent);

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
            calendarEvent);

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.Null(result.Content!.Description);
    }

    [Fact]
    public async Task RenderAsync_TemplateIds_RendersTemplateContent()
    {
        var renderer = new PublishingContentRenderer(
            new FakeTemplateReader(
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

        var result = await renderer.RenderAsync(
            Platform(new PublishingContent("title-template", "description-template")),
            Event(),
            CancellationToken.None);

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.Equal("English title on 2026-06-05", result.Content!.Title);
        Assert.Equal("Details: English description", result.Content.Description);
    }

    [Fact]
    public async Task RenderAsync_MissingTemplate_ReturnsTemplateNotFound()
    {
        var renderer = new PublishingContentRenderer(new FakeTemplateReader());

        var result = await renderer.RenderAsync(
            Platform(new PublishingContent("missing-template", "description-template")),
            Event(),
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
                new EventTextField("text1", "Title", EventTextType.ShortText, 50),
                new EventTextField("text2", "Description", EventTextType.LongText, 2500),
                new EventTextField("text3", "Russian title", EventTextType.ShortText, 50),
                new EventTextField("text4", "Russian description", EventTextType.LongText, 2500)
            ],
            [
                new EventTextValue("text1", "English title"),
                new EventTextValue("text2", description ?? string.Empty),
                new EventTextValue("text3", "Russian title"),
                new EventTextValue("text4", "Russian description")
            ]);

    private static PlatformView Platform(PublishingContent publishingContent) =>
        new(
            "platform-id",
            "Main channel",
            null,
            PlatformType.YouTube,
            new YouTubeSettings(
                new YouTubeCredentials("client-id", "client-secret", "refresh-token"),
                "private",
                false),
            publishingContent);

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
