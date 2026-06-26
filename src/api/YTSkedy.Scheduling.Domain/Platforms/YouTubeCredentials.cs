namespace YTSkedy.Scheduling.Domain.Platforms;

/// <summary>
/// Secret-bearing Google OAuth credential material for one YouTube platform.
/// HTTP responses, logs, and publication snapshots must use redacted
/// projections instead of exposing this object.
/// </summary>
public sealed record YouTubeCredentials
{
    public YouTubeCredentials(
        string clientId,
        string clientSecret,
        string refreshToken)
    {
        if (!IsValidClientId(clientId))
        {
            throw new ArgumentException("Client ID must be non-empty.", nameof(clientId));
        }

        if (!IsValidClientSecret(clientSecret))
        {
            throw new ArgumentException(
                "Client secret must be non-empty.",
                nameof(clientSecret));
        }

        if (!IsValidRefreshToken(refreshToken))
        {
            throw new ArgumentException(
                "Refresh token must be non-empty.",
                nameof(refreshToken));
        }

        ClientId = clientId.Trim();
        ClientSecret = clientSecret;
        RefreshToken = refreshToken;
    }

    public string ClientId { get; }

    public string ClientSecret { get; }

    public string RefreshToken { get; }

    public static bool IsValidClientId(string? clientId) =>
        !string.IsNullOrWhiteSpace(clientId);

    public static bool IsValidClientSecret(string? clientSecret) =>
        !string.IsNullOrWhiteSpace(clientSecret);

    public static bool IsValidRefreshToken(string? refreshToken) =>
        !string.IsNullOrWhiteSpace(refreshToken);
}
