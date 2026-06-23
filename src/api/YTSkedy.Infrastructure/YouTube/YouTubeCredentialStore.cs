using Microsoft.Extensions.Options;

namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Configuration-backed <see cref="IYouTubeCredentialStore"/>. Looks up
/// the credentials reference name (case-insensitively) in the bound
/// <see cref="YouTubeOptions"/> and returns the channel secrets only when
/// the entry exists and every required field is present. Missing or incomplete
/// entries resolve to null so the caller surfaces a clear "not configured"
/// failure instead of attempting a broken provider call.
/// </summary>
public sealed class YouTubeCredentialStore(
    IOptions<YouTubeOptions> options) : IYouTubeCredentialStore
{
    public YouTubeCredentials? Find(string credentialsReference)
    {
        if (string.IsNullOrWhiteSpace(credentialsReference))
        {
            return null;
        }

        var match = options.Value.FirstOrDefault(entry =>
            string.Equals(entry.Key, credentialsReference, StringComparison.OrdinalIgnoreCase));

        var credentials = match.Value;
        if (credentials is null ||
            string.IsNullOrWhiteSpace(credentials.ClientId) ||
            string.IsNullOrWhiteSpace(credentials.ClientSecret) ||
            string.IsNullOrWhiteSpace(credentials.RefreshToken))
        {
            return null;
        }

        return credentials;
    }
}
