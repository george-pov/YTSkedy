using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Infrastructure.Templates;

internal static class TemplateViewMapper
{
    internal static TemplateView ToView(TemplateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TemplateView(
            entity.TemplateId,
            entity.Name,
            ParseType(entity.Type),
            entity.Content);
    }

    internal static IReadOnlyList<TemplateView> ToViews(IEnumerable<TemplateEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(ToView)
            .ToArray();
    }

    internal static TemplateType ParseType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "youtube" => TemplateType.YouTube,
            "wordpress" => TemplateType.WordPress,
            _ => throw InvalidStoredValue(nameof(TemplateType), type)
        };

    private static InvalidOperationException InvalidStoredValue(string fieldName, string? value) =>
        new($"Stored {fieldName} value '{value ?? "<null>"}' is invalid.");
}
