namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// YouTube-specific <see cref="PublishSettings"/> for a YouTube
/// <see cref="Platform"/>. Carries secret-bearing Google OAuth credential
/// material and the broadcast defaults applied when publishing. This type must
/// be redacted from HTTP responses and sanitized before publication snapshots
/// are written.
/// </summary>
public sealed record YouTubeSettings : PublishSettings
{
    public static readonly IReadOnlyList<string> AllowedPrivacyStatuses =
        ["private", "public", "unlisted"];

    public YouTubeSettings(
        YouTubeCredentials credentials,
        string privacyStatus,
        bool selfDeclaredMadeForKids,
        string? categoryId = null,
        bool containsSyntheticMedia = false)
    {
        ArgumentNullException.ThrowIfNull(credentials);

        if (!IsValidPrivacyStatus(privacyStatus))
        {
            throw new ArgumentException(
                "Privacy status must be 'private', 'public', or 'unlisted'.",
                nameof(privacyStatus));
        }

        if (!IsValidCategoryId(categoryId))
        {
            throw new ArgumentException(
                "Category id must be null or contain a non-blank YouTube category id.",
                nameof(categoryId));
        }

        Credentials = credentials;
        PrivacyStatus = privacyStatus;
        SelfDeclaredMadeForKids = selfDeclaredMadeForKids;
        CategoryId = categoryId?.Trim();
        ContainsSyntheticMedia = containsSyntheticMedia;
    }

    public YouTubeCredentials Credentials { get; }

    public string PrivacyStatus { get; }

    public bool SelfDeclaredMadeForKids { get; }

    public string? CategoryId { get; }

    public bool ContainsSyntheticMedia { get; }

    /// <summary>
    /// True when <paramref name="privacyStatus"/> is one of the lowercase
    /// YouTube privacy values (<c>private</c>, <c>public</c>, <c>unlisted</c>).
    /// </summary>
    public static bool IsValidPrivacyStatus(string? privacyStatus) =>
        privacyStatus is not null &&
        AllowedPrivacyStatuses.Contains(privacyStatus, StringComparer.Ordinal);

    public static bool IsValidCategoryId(string? categoryId) =>
        categoryId is null || !string.IsNullOrWhiteSpace(categoryId);
}
