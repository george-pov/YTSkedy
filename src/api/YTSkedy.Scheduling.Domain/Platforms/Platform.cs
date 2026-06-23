namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Create-input for a configured publishing destination. Carries the editable
/// name, the immutable provider type, and the type-specific publish settings.
/// Mirrors <see cref="Templates.Template"/>: it holds no persisted id because
/// the repository generates the id on create. The constructor enforces the same
/// non-empty and length rule the API boundary exposes through
/// <see cref="IsValidName"/>, so the boundary and the domain share one source of
/// truth. The name is trimmed so stored and compared names are stable.
/// </summary>
public sealed class Platform
{
    public const int MaxNameLength = 50;

    public Platform(string name, PlatformType type, PublishSettings publishSettings)
    {
        ArgumentNullException.ThrowIfNull(publishSettings);

        if (!IsValidName(name))
        {
            throw new ArgumentException(
                $"Name must be non-empty and at most {MaxNameLength} characters.",
                nameof(name));
        }

        Name = name.Trim();
        Type = type;
        PublishSettings = publishSettings;
    }

    public string Name { get; }

    public PlatformType Type { get; }

    public PublishSettings PublishSettings { get; }

    /// <summary>
    /// True when <paramref name="name"/> is non-empty (ignoring surrounding
    /// whitespace) and at most <see cref="MaxNameLength"/> characters after
    /// trimming. The API boundary uses this to reject input with
    /// <c>400 Bad Request</c> before constructing a <see cref="Platform"/>.
    /// </summary>
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= MaxNameLength;
}
