using YTSkedy.Scheduling.Domain.CalendarEvents;
using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Scheduling.Domain.Templates;

/// <summary>
/// Single source of truth for the placeholder tokens available to template
/// content. Text tokens come from the current event text field list, date tokens
/// are code-defined, and platform reference-key tokens come from active
/// platforms.
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

    public static IReadOnlyList<TemplateToken> From(
        EventTextFields fields,
        IEnumerable<string?> platformReferenceKeys)
    {
        ArgumentNullException.ThrowIfNull(platformReferenceKeys);

        var tokens = From(fields);
        var tokenNames = tokens
            .Select(token => token.Name)
            .ToHashSet(StringComparer.Ordinal);
        var referenceKeyTokens = platformReferenceKeys
            .Select(Platform.NormalizeReferenceKey)
            .OfType<string>()
            .Where(referenceKey => !tokenNames.Contains(referenceKey))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(referenceKey => new TemplateToken(referenceKey));

        return tokens
            .Concat(referenceKeyTokens)
            .ToArray();
    }
}
