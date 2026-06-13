using System.ComponentModel.DataAnnotations;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Predefined static Google OAuth credentials used to publish broadcasts.
/// These are secrets: bound from the <c>YouTube</c> configuration section and
/// never committed to source control or logged. The backend exchanges the
/// refresh token for short-lived access tokens at runtime.
/// </summary>
public sealed class YouTubeOptions
{
    public const string SectionName = "YouTube";

    /// <summary>
    /// Google OAuth 2.0 client identifier.
    /// </summary>
    [Required]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Google OAuth 2.0 client secret.
    /// </summary>
    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Long-lived Google OAuth 2.0 refresh token minted once during setup with
    /// the YouTube scope. Used to silently obtain access tokens for the single
    /// channel that owns it.
    /// </summary>
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
