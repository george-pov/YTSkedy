using YTSkedy.Scheduling.Domain.CalendarEvents;

namespace YTSkedy.Scheduling.Domain.Templates;

/// <summary>
/// Single source of truth for the placeholder tokens available to template
/// content. Text tokens come from the current event text field list, while date
/// tokens are code-defined.
/// </summary>
public static class TemplateTokenCatalog
{
    public static IReadOnlyList<TemplateToken> DateTokens { get; } =
    [
        new TemplateToken("longDateEn"),
        new TemplateToken("shortDateEn"),
        new TemplateToken("longDateRu"),
        new TemplateToken("shortDateRu"),
        new TemplateToken("longDateFr"),
        new TemplateToken("shortDateFr")
    ];

    public static IReadOnlyList<TemplateToken> From(EventTextFields fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        return fields.Fields
            .Select(field => new TemplateToken(field.FieldKey))
            .Concat(DateTokens)
            .ToArray();
    }
}
