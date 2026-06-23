namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Read model for a stored platform. Carries the persisted id alongside the
/// editable name, provider type, and publish settings. Mirrors
/// <see cref="Templates.TemplateView"/> as the read counterpart to the
/// <see cref="Platform"/> create-input.
/// </summary>
public sealed class PlatformView
{
    public PlatformView(
        string platformId,
        string name,
        PlatformType type,
        PublishSettings publishSettings)
    {
        PlatformId = platformId;
        Name = name;
        Type = type;
        PublishSettings = publishSettings;
    }

    public string PlatformId { get; }

    public string Name { get; }

    public PlatformType Type { get; }

    public PublishSettings PublishSettings { get; }
}
