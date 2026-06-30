using YTSkedy.Scheduling.Application.Platforms;
using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Application.Test;

public class PublishingContentRendererTests
{
    [Fact]
    public void Render_KnownTokens_ReplacesPlaceholders()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ title }} on {{ shortDate }}",
            "Details: {{ description }}",
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
            "{{    titleRu    }}",
            null,
            Event());

        Assert.Equal("Russian title", result.Content!.Title);
    }

    [Fact]
    public void Render_RepeatedTokens_ReplacesEveryOccurrence()
    {
        var renderer = new PublishingContentRenderer();

        var result = renderer.Render(
            "{{ title }} / {{ title }}",
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
            "{ title } and {{ title",
            "Text {{ title ru }}",
            Event());

        Assert.Equal("{ title } and {{ title", result.Content!.Title);
        Assert.Equal("Text {{ title ru }}", result.Content.Description);
        Assert.False(result.HasUnresolvedPlaceholders);
    }

    [Fact]
    public void Render_EmptyRenderedTitle_ReturnsEmptyTitle()
    {
        var renderer = new PublishingContentRenderer();
        var calendarEvent = Event(description: null);

        var result = renderer.Render(
            "{{ description }}",
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
            "{{ title }}",
            "{{ description }}",
            calendarEvent);

        Assert.Equal(RenderContentStatus.Rendered, result.Status);
        Assert.Null(result.Content!.Description);
    }

    private static CalendarEventView Event(string? description = "English description") =>
        new(
            "calendar-event-id",
            new ScheduledStart(new DateTime(2026, 6, 5, 10, 30, 0), "America/Vancouver"),
            new DateTimeOffset(2026, 6, 5, 17, 30, 0, TimeSpan.Zero),
            [
                new LocalizedDescription("en", "English title", description),
                new LocalizedDescription("ru", "Russian title", "Russian description")
            ]);
}
