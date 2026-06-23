using YTSkedy.Scheduling.Domain.Templates;

namespace YTSkedy.Infrastructure.Templates;

internal static class TemplateViewMapper
{
    public static TemplateView ToView(TemplateEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TemplateView(
            entity.TemplateId,
            entity.Name,
            ParseType(entity.Type),
            entity.Content);
    }

    public static IReadOnlyList<TemplateView> ToViews(IEnumerable<TemplateEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(ToView)
            .ToArray();
    }

    internal static TemplateType ParseType(string? type) =>
        Enum.TryParse<TemplateType>(type, ignoreCase: true, out var parsed)
            ? parsed
            : TemplateType.YouTube;
}