namespace YTSkedy.Infrastructure.YouTube;

/// <summary>
/// Resolves a platform's non-secret credentials reference name to the Google
/// OAuth secrets for that YouTube channel. Returns null when the reference is not
/// configured, so the publisher can fail the publish without leaking which
/// secrets exist.
/// </summary>
public interface IYouTubeCredentialStore
{
    YouTubeCredentials? Find(string credentialsReference);
}
