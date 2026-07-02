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
        return await RenderAsync(
            platform,
            calendarEvent,
            runtimeTokenValues: null,
            cancellationToken);
    }

    public async Task<RenderContentResult> RenderAsync(
        PlatformView platform,
        CalendarEventView calendarEvent,
        IReadOnlyDictionary<string, string>? runtimeTokenValues,
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

        return Render(title, description, calendarEvent, runtimeTokenValues);
    }

    public RenderContentResult Render(
        string title,
        string? description,
        CalendarEventView calendarEvent,
        IReadOnlyDictionary<string, string>? runtimeTokenValues = null)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(calendarEvent);

        var tokenValues = MergeTokenValues(
            CalendarEventTokenValues.From(calendarEvent).Values,
            runtimeTokenValues);
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

    private static IReadOnlyDictionary<string, string> MergeTokenValues(
        IReadOnlyDictionary<string, string> calendarEventTokenValues,
        IReadOnlyDictionary<string, string>? runtimeTokenValues)
    {
        if (runtimeTokenValues is null || runtimeTokenValues.Count == 0)
        {
            return calendarEventTokenValues;
        }

        var values = new Dictionary<string, string>(
            calendarEventTokenValues,
            StringComparer.Ordinal);

        foreach (var (tokenName, tokenValue) in runtimeTokenValues)
        {
            values.TryAdd(tokenName, tokenValue);
        }

        return values;
    }

    private static bool HasWellFormedPlaceholder(string? text) =>
        text is not null && Placeholder().IsMatch(text);

    [GeneratedRegex(@"\{\{\s*(?<token>[^{}\s]+)\s*\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex Placeholder();
}
