using YTSkedy.Scheduling.Domain.Platforms;

namespace YTSkedy.Infrastructure.Platforms;

internal static class PlatformReadMapper
{
    public static PlatformView ToView(PlatformEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var type = ParseType(entity.Type);
        var publishSettings = PlatformPublishSettingsSerializer.Deserialize(
            type,
            entity.PublishSettingsJson);

        return new PlatformView(
            entity.PlatformId,
            entity.Name,
            type,
            publishSettings);
    }

    public static IReadOnlyList<PlatformView> ToViews(IEnumerable<PlatformEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return entities
            .Select(ToView)
            .ToArray();
    }

    internal static PlatformType ParseType(string? type) =>
        Enum.TryParse<PlatformType>(type, ignoreCase: true, out var parsed)
            ? parsed
            : PlatformType.YouTube;
}
