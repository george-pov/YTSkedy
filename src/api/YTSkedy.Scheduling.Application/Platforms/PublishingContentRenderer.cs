using System.Text.RegularExpressions;
using YTSkedy.Scheduling.Application.Templates;
using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Application.Platforms;

/// <summary>
/// Renders title and description template text with calendar-event token values.
/// Unknown well-formed placeholders are preserved so preview can show them and
/// publish can reject them before a provider call.
/// </summary>
public sealed partial class PublishingContentRenderer
{
    private readonly ITemplateReader? templates;

    public PublishingContentRenderer()
    {
    }

    public PublishingContentRenderer(ITemplateReader templates)
    {
        ArgumentNullException.ThrowIfNull(templates);

        this.templates = templates;
    }

    public async Task<RenderContentResult> RenderAsync(
        PlatformView platform,
        CalendarEventView calendarEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(calendarEvent);

        if (templates is null)
        {
            throw new InvalidOperationException(
                "A template reader is required to render platform publishing content.");
        }

        var title = await ReadTemplateContentAsync(
            platform.Type,
            platform.PublishingContent.TitleTemplateId,
            cancellationToken);
        if (title is null)
        {
            return RenderContentResult.TemplateNotFound();
        }

        var description = await ReadTemplateContentAsync(
            platform.Type,
            platform.PublishingContent.DescriptionTemplateId,
            cancellationToken);
        if (description is null)
        {
            return RenderContentResult.TemplateNotFound();
        }

        return Render(title, description, calendarEvent);
    }

    public RenderContentResult Render(
        string title,
        string? description,
        CalendarEventView calendarEvent)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(calendarEvent);

        var tokenValues = CalendarEventTokenValues.From(calendarEvent).Values;
        var renderedTitle = RenderText(title, tokenValues);
        var renderedDescription = description is null
            ? null
            : RenderText(description, tokenValues);

        var hasUnresolvedPlaceholders =
            HasWellFormedPlaceholder(renderedTitle) ||
            HasWellFormedPlaceholder(renderedDescription);

        if (string.IsNullOrWhiteSpace(renderedTitle))
        {
            return RenderContentResult.EmptyTitle(hasUnresolvedPlaceholders);
        }

        return RenderContentResult.Rendered(
            new RenderedContent(renderedTitle, renderedDescription),
            hasUnresolvedPlaceholders);
    }

    private static string RenderText(
        string text,
        IReadOnlyDictionary<string, string> tokenValues) =>
        Placeholder().Replace(
            text,
            match =>
            {
                var tokenName = match.Groups["token"].Value;

                return tokenValues.TryGetValue(tokenName, out var tokenValue)
                    ? tokenValue
                    : match.Value;
            });

    private async Task<string?> ReadTemplateContentAsync(
        PlatformType platformType,
        string templateId,
        CancellationToken cancellationToken)
    {
        var template = await templates!.GetAsync(
            TemplateLinkValidator.ToTemplateType(platformType),
            templateId,
            cancellationToken);

        return template?.Content;
    }

    private static bool HasWellFormedPlaceholder(string? text) =>
        text is not null && Placeholder().IsMatch(text);

    [GeneratedRegex(@"\{\{\s*(?<token>[^{}\s]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
