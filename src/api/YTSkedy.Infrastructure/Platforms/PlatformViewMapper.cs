using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

internal static class PlatformViewMapper
{
    internal static PlatformView ToView(PlatformEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var type = ParseType(entity.Type);
        var publishSettings = PublishSettingsSerializer.Deserialize(
            type,
            entity.PublishSettingsJson);

        return new PlatformView(
            entity.PlatformId,
            entity.Name,
            entity.ReferenceKey,
            type,
            publishSettings,
            new PublishingContent(
                entity.TitleTemplateId,
                entity.DescriptionTemplateId));
    }

    internal static IReadOnlyList<PlatformView> ToViews(IEnumerable<PlatformEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(ToView)
            .ToArray();
    }

    internal static PlatformType ParseType(string? type) =>
        type?.ToLowerInvariant() switch
        {
            "youtube" => PlatformType.YouTube,
            "wordpress" => PlatformType.WordPress,
            _ => throw InvalidStoredValue(nameof(PlatformType), type)
        };

    private static InvalidOperationException InvalidStoredValue(string fieldName, string? value) =>
        new($"Stored {fieldName} value '{value ?? "<null>"}' is invalid.");
}
