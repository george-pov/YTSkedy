namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// YouTube-specific <see cref="PublishSettings"/> for a YouTube
/// <see cref="Platform"/>. Carries the non-secret credential reference and the
/// broadcast defaults applied when publishing. The constructor enforces the same
/// rules the API boundary exposes through <see cref="IsValidCredentials"/> and
/// <see cref="IsValidPrivacyStatus"/>, so the boundary and the domain share one
/// source of truth. <see cref="Credentials"/> is a non-secret name for
/// externally configured credential material; the actual secrets are never
/// stored here.
/// </summary>
public sealed record YouTubePublishSettings : PublishSettings
{
    public static readonly IReadOnlyList<string> AllowedPrivacyStatuses =
        ["private", "public", "unlisted"];

    public YouTubePublishSettings(
        string credentials,
        string privacyStatus,
        bool selfDeclaredMadeForKids)
    {
        if (!IsValidCredentials(credentials))
        {
            throw new ArgumentException(
                "Credentials must be a non-empty reference name.",
                nameof(credentials));
        }

        if (!IsValidPrivacyStatus(privacyStatus))
        {
            throw new ArgumentException(
                "Privacy status must be 'private', 'public', or 'unlisted'.",
                nameof(privacyStatus));
        }

        Credentials = credentials;
        PrivacyStatus = privacyStatus;
        SelfDeclaredMadeForKids = selfDeclaredMadeForKids;
    }

    public string Credentials { get; }

    public string PrivacyStatus { get; }

    public bool SelfDeclaredMadeForKids { get; }

    /// <summary>
    /// True when <paramref name="credentials"/> is a non-empty reference name.
    /// The API boundary uses this to reject input with <c>400 Bad Request</c>
    /// before constructing settings.
    /// </summary>
    public static bool IsValidCredentials(string? credentials) =>
        !string.IsNullOrWhiteSpace(credentials);

    /// <summary>
    /// True when <paramref name="privacyStatus"/> is one of the lowercase
    /// YouTube privacy values (<c>private</c>, <c>public</c>, <c>unlisted</c>).
    /// </summary>
    public static bool IsValidPrivacyStatus(string? privacyStatus) =>
        privacyStatus is not null &&
        AllowedPrivacyStatuses.Contains(privacyStatus, StringComparer.Ordinal);
}
