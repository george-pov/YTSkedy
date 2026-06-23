namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Static Google OAuth credentials for one YouTube channel, resolved by a
/// platform's non-secret credentials reference name. These are secrets bound
/// from configuration and never logged or written to application storage. The
/// backend exchanges the refresh token for short-lived access tokens at runtime.
/// </summary>
public sealed class YouTubeCredentials
{
    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;
}
