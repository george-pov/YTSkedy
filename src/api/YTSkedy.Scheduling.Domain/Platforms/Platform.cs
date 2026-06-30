namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Create-input for a configured publishing destination. Carries the editable
/// name, the immutable provider type, platform-owned publishing content, and
/// the type-specific publish settings. Mirrors <see cref="Templates.Template"/>:
/// it holds no persisted id because the repository generates the id on create.
/// The constructor enforces the same non-empty name rule and optional
/// reference-key rule the API boundary exposes through <see cref="IsValidName"/>
/// and <see cref="IsValidReferenceKey"/>, so the boundary and the domain share
/// one source of truth. The name and reference key are trimmed so stored and
/// compared values are stable.
/// </summary>
public sealed class Platform
{
    public const int MaxNameLength = 50;

    public const int MaxReferenceKeyLength = 15;

    public Platform(
        string name,
        PlatformType type,
        PublishSettings publishSettings,
        string? referenceKey = null,
        PublishingContent? publishingContent = null)
    {
        ArgumentNullException.ThrowIfNull(publishSettings);

        if (!IsValidName(name))
        {
            throw new ArgumentException(
                $"Name must be non-empty and at most {MaxNameLength} characters.",
                nameof(name));
        }

        if (!IsValidReferenceKey(referenceKey))
        {
            throw new ArgumentException(
                "Reference key must be 1 to 15 characters and contain only letters, numbers, or hyphen.",
                nameof(referenceKey));
        }

        Name = name.Trim();
        Type = type;
        PublishSettings = publishSettings;
        ReferenceKey = NormalizeReferenceKey(referenceKey);
        PublishingContent = publishingContent ?? PublishingContent.None;
    }

    public string Name { get; }

    public PlatformType Type { get; }

    public PublishSettings PublishSettings { get; }

    public string? ReferenceKey { get; }

    public PublishingContent PublishingContent { get; }

    /// <summary>
    /// True when <paramref name="name"/> is non-empty (ignoring surrounding
    /// whitespace) and at most <see cref="MaxNameLength"/> characters after
    /// trimming. The API boundary uses this to reject input with
    /// <c>400 Bad Request</c> before constructing a <see cref="Platform"/>.
    /// </summary>
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= MaxNameLength;

    /// <summary>
    /// Trims an optional user-facing platform reference key. Missing, empty, or
    /// whitespace-only input means no key and is represented as <c>null</c>.
    /// </summary>
    public static string? NormalizeReferenceKey(string? referenceKey)
    {
        var trimmed = referenceKey?.Trim();

        return string.IsNullOrEmpty(trimmed)
            ? null
            : trimmed;
    }

    /// <summary>
    /// True when <paramref name="referenceKey"/> is missing or blank, or when
    /// the trimmed key is 1 to <see cref="MaxReferenceKeyLength"/> ASCII
    /// letters, digits, or hyphen characters.
    /// </summary>
    public static bool IsValidReferenceKey(string? referenceKey)
    {
        var normalized = NormalizeReferenceKey(referenceKey);
        if (normalized is null)
        {
            return true;
        }

        if (normalized.Length > MaxReferenceKeyLength)
        {
            return false;
        }

        foreach (var character in normalized)
        {
            if (!IsReferenceKeyCharacter(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Converts a non-empty reference key to the value used for
    /// case-insensitive lookup and uniqueness checks.
    /// </summary>
    public static string ToReferenceKeyLookupValue(string referenceKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceKey);

        return referenceKey.Trim().ToLowerInvariant();
    }

    private static bool IsReferenceKeyCharacter(char character) =>
        character is >= 'A' and <= 'Z'
            or >= 'a' and <= 'z'
            or >= '0' and <= '9'
            or '-';
}
